using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PhiraMpServer.Common;

/// <summary>
/// Bidirectional TCP stream with framing protocol
/// Compatible with Rust phira-mp-common Stream implementation
/// </summary>
public class ProtocolStream : IDisposable
{
    private const int MaxPacketSize = 2 * 1024 * 1024; // 2MB
    private readonly NetworkStream _networkStream;
    private readonly Channel<ServerCommand> _sendChannel;
    private readonly Task _sendTask;
    private readonly Task _receiveTask;
    private readonly CancellationTokenSource _cts;
    private readonly Func<ServerCommand, Task> _handler;
    private readonly ILogger? _logger;
    private bool _disposed;

    public byte Version { get; }

    public ProtocolStream(
        TcpClient client,
        Func<ServerCommand, Task> handler,
        ILogger? logger = null)
    {
        _networkStream = client.GetStream();
        _handler = handler;
        _logger = logger;
        _cts = new CancellationTokenSource();
        _sendChannel = Channel.CreateUnbounded<ServerCommand>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Set TCP_NODELAY for low latency
        client.NoDelay = true;

        // Server sends version
        Version = 0; // Protocol version
        _networkStream.WriteByte(Version);
        _networkStream.Flush();

        _sendTask = Task.Run(SendLoop, _cts.Token);
        _receiveTask = Task.Run(ReceiveLoop, _cts.Token);
    }

    public async Task SendAsync(ServerCommand command)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ProtocolStream));

        await _sendChannel.Writer.WriteAsync(command, _cts.Token);
    }

    public bool TrySend(ServerCommand command)
    {
        if (_disposed)
            return false;

        return _sendChannel.Writer.TryWrite(command);
    }

    private async Task SendLoop()
    {
        var buffer = new List<byte>();
        var lengthBuffer = new byte[5];

        try
        {
            await foreach (var command in _sendChannel.Reader.ReadAllAsync(_cts.Token))
            {
                buffer.Clear();

                // Serialize command
                var writer = new BinaryWriter();
                command.WriteBinary(writer);
                var payload = writer.ToArray();

                _logger?.LogTrace("Sending {Size} bytes ({Command})", payload.Length, command.GetType().Name);

                // Write ULEB128 length
                uint length = (uint)payload.Length;
                int lengthSize = 0;

                do
                {
                    byte b = (byte)(length & 0x7F);
                    length >>= 7;
                    if (length != 0)
                        b |= 0x80;
                    lengthBuffer[lengthSize++] = b;
                } while (length != 0);

                // Send length + payload
                await _networkStream.WriteAsync(lengthBuffer.AsMemory(0, lengthSize), _cts.Token);
                await _networkStream.WriteAsync(payload, _cts.Token);
                await _networkStream.FlushAsync(_cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in send loop");
        }
    }

    private async Task ReceiveLoop()
    {
        var buffer = new byte[MaxPacketSize];

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                // Read ULEB128 length
                uint length = 0;
                int shift = 0;

                while (true)
                {
                    int b = await ReadByteAsync(_cts.Token);
                    if (b == -1)
                    {
                        _logger?.LogDebug("Connection closed by peer");
                        return;
                    }

                    length |= ((uint)(b & 0x7F)) << shift;
                    shift += 7;

                    if ((b & 0x80) == 0)
                        break;

                    if (shift > 32)
                    {
                        _logger?.LogWarning("Invalid length encoding");
                        return;
                    }
                }

                if (length > MaxPacketSize)
                {
                    _logger?.LogWarning("Packet too large: {Size}", length);
                    return;
                }

                // Read payload
                await ReadExactAsync(buffer.AsMemory(0, (int)length), _cts.Token);

                _logger?.LogTrace("Received {Size} bytes", length);

                // Parse command
                try
                {
                    var reader = new BinaryReader(buffer.AsSpan(0, (int)length).ToArray());
                    var command = ClientCommand.ReadBinary(reader);

                    _logger?.LogTrace("Decoded to {Command}", command.GetType().Name);

                    // Handle command
                    await HandleCommandAsync(command);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Invalid packet");
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in receive loop");
        }
    }

    private async Task HandleCommandAsync(ClientCommand command)
    {
        try
        {
            // Special handling for Ping
            if (command is PingCommand)
            {
                await SendAsync(new PongCommand());
                return;
            }

            // Convert ClientCommand to ServerCommand for handler
            // The handler will process and send appropriate response
            // This is a placeholder - actual implementation in Session class
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling command {Command}", command.GetType().Name);
        }
    }

    private async Task<int> ReadByteAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        int read = await _networkStream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);
        return read == 0 ? -1 : buffer[0];
    }

    private async Task ReadExactAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await _networkStream.ReadAsync(buffer.Slice(totalRead), cancellationToken);
            if (read == 0)
                throw new EndOfStreamException("Connection closed");
            totalRead += read;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cts.Cancel();
        _sendChannel.Writer.Complete();

        try
        {
            Task.WhenAll(_sendTask, _receiveTask).Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            // Ignore
        }

        _cts.Dispose();
        _networkStream.Dispose();
    }
}

/// <summary>
/// Stream wrapper specifically for handling client commands
/// </summary>
public class ClientStream : IDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _networkStream;
    private readonly Channel<ServerCommand> _sendChannel;
    private readonly Task _sendTask;
    private readonly Task _receiveTask;
    private readonly CancellationTokenSource _cts;
    private readonly Func<ClientCommand, Task<ServerCommand?>> _handler;
    private readonly ILogger? _logger;
    private DateTime _lastReceive;
    private bool _disposed;

    public byte Version { get; }
    public DateTime LastReceive => _lastReceive;

    public ClientStream(
        TcpClient client,
        Func<ClientCommand, Task<ServerCommand?>> handler,
        ILogger? logger = null)
    {
        _client = client;
        _networkStream = client.GetStream();
        _handler = handler;
        _logger = logger;
        _cts = new CancellationTokenSource();
        _lastReceive = DateTime.UtcNow;
        _sendChannel = Channel.CreateUnbounded<ServerCommand>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Set TCP_NODELAY for low latency
        client.NoDelay = true;

        // Server reads version from client (client sends version byte first)
        int versionByte = _networkStream.ReadByte();
        if (versionByte == -1)
        {
            throw new InvalidOperationException("Failed to read version from client");
        }
        Version = (byte)versionByte;
        _logger?.LogDebug("Client version: {Version}", Version);

        _sendTask = Task.Run(SendLoop, _cts.Token);
        _receiveTask = Task.Run(ReceiveLoop, _cts.Token);
    }

    public async Task SendAsync(ServerCommand command)
    {
        if (_disposed)
            return;

        await _sendChannel.Writer.WriteAsync(command, _cts.Token);
    }

    public bool TrySend(ServerCommand command)
    {
        if (_disposed)
            return false;

        return _sendChannel.Writer.TryWrite(command);
    }

    private async Task SendLoop()
    {
        var lengthBuffer = new byte[5];

        try
        {
            await foreach (var command in _sendChannel.Reader.ReadAllAsync(_cts.Token))
            {
                // Serialize command
                var writer = new BinaryWriter();
                command.WriteBinary(writer);
                var payload = writer.ToArray();

                _logger?.LogTrace("Sending {Size} bytes ({Command}): {Payload}",
                    payload.Length, command.GetType().Name, BitConverter.ToString(payload));

                // Write ULEB128 length
                uint length = (uint)payload.Length;
                int lengthSize = 0;

                do
                {
                    byte b = (byte)(length & 0x7F);
                    length >>= 7;
                    if (length != 0)
                        b |= 0x80;
                    lengthBuffer[lengthSize++] = b;
                } while (length != 0);

                // Send length + payload
                await _networkStream.WriteAsync(lengthBuffer.AsMemory(0, lengthSize), _cts.Token);
                await _networkStream.WriteAsync(payload, _cts.Token);
                await _networkStream.FlushAsync(_cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in send loop");
        }
    }

    private async Task ReceiveLoop()
    {
        var buffer = new byte[2 * 1024 * 1024];

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                // Read ULEB128 length
                uint length = 0;
                int shift = 0;

                while (true)
                {
                    int b = await ReadByteAsync(_cts.Token);
                    if (b == -1)
                    {
                        _logger?.LogDebug("Connection closed by peer");
                        return;
                    }

                    length |= ((uint)(b & 0x7F)) << shift;
                    shift += 7;

                    if ((b & 0x80) == 0)
                        break;

                    if (shift > 32)
                    {
                        _logger?.LogWarning("Invalid length encoding");
                        return;
                    }
                }

                if (length > 2 * 1024 * 1024)
                {
                    _logger?.LogWarning("Packet too large: {Size}", length);
                    return;
                }

                // Read payload
                await ReadExactAsync(buffer.AsMemory(0, (int)length), _cts.Token);
                _lastReceive = DateTime.UtcNow;

                _logger?.LogTrace("Received {Size} bytes: {Payload}",
                    length, BitConverter.ToString(buffer, 0, (int)length));

                // Parse command
                try
                {
                    var reader = new BinaryReader(buffer.AsSpan(0, (int)length).ToArray());
                    var command = ClientCommand.ReadBinary(reader);

                    _logger?.LogTrace("Decoded to {Command}", command.GetType().Name);

                    // Handle command
                    await HandleCommandAsync(command);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Invalid packet: {Payload}",
                        BitConverter.ToString(buffer, 0, Math.Min((int)length, 100)));
                    // Continue processing instead of terminating connection
                    // This allows recovery from corrupted packets
                    continue;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in receive loop");
        }
    }

    private async Task HandleCommandAsync(ClientCommand command)
    {
        try
        {
            // Special handling for Ping
            if (command is PingCommand)
            {
                await SendAsync(new PongCommand());
                return;
            }

            // Call handler and send response if any
            var response = await _handler(command);
            if (response != null)
            {
                await SendAsync(response);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling command {Command}", command.GetType().Name);
        }
    }

    private async Task<int> ReadByteAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        int read = await _networkStream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);
        return read == 0 ? -1 : buffer[0];
    }

    private async Task ReadExactAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await _networkStream.ReadAsync(buffer.Slice(totalRead), cancellationToken);
            if (read == 0)
                throw new EndOfStreamException("Connection closed");
            totalRead += read;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cts.Cancel();
        _sendChannel.Writer.Complete();

        try
        {
            Task.WhenAll(_sendTask, _receiveTask).Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            // Ignore
        }

        _cts.Dispose();
        _networkStream.Dispose();
        _client.Dispose();
    }
}
