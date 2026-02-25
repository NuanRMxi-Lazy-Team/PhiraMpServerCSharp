using System.Buffers;
using System.Net.Sockets;
using System.Threading.Channels;
using PhiraMp.Core;
using BinaryReader = PhiraMp.Core.BinaryReader;
using BinaryWriter = PhiraMp.Core.BinaryWriter;

namespace PhiraMp.Network;

/// <summary>
/// Stream wrapper specifically for handling client commands
/// </summary>
public class ClientStream : INetworkSession
{
    private readonly TcpClient _client;
    private readonly NetworkStream _networkStream;
    private readonly Channel<ServerCommand> _sendChannel;
    private readonly Task _sendTask;
    private readonly Task _receiveTask;
    private readonly CancellationTokenSource _cts;
    private readonly Func<ClientCommand, Task<ServerCommand?>> _handler;
    private DateTime _lastReceive;
    private bool _disposed;
    private volatile bool _isConnected = true;

    public byte Version { get; }
    public DateTime LastReceive => _lastReceive;
    public bool IsConnected => _isConnected && !_disposed && _client.Connected;

    public ClientStream(
        TcpClient client,
        Func<ClientCommand, Task<ServerCommand?>> handler)
    {
        _client = client;
        _networkStream = client.GetStream();
        _handler = handler;
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
        // Rent reusable buffers from ArrayPool to reduce allocations
        byte[] lengthBuffer = ArrayPool<byte>.Shared.Rent(5);

        try
        {
            await foreach (var command in _sendChannel.Reader.ReadAllAsync(_cts.Token))
            {
                try
                {
                    // Serialize command
                    var writer = new BinaryWriter();
                    command.WriteBinary(writer);
                    var payload = writer.ToArray();

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

                    // Send length + payload in one operation
                    await _networkStream.WriteAsync(lengthBuffer.AsMemory(0, lengthSize), _cts.Token);
                    await _networkStream.WriteAsync(payload, _cts.Token);
                    await _networkStream.FlushAsync(_cts.Token);
                }
                catch (Exception)
                {
                    _isConnected = false;
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (IOException ioEx) when (ioEx.InnerException is SocketException socketEx &&
                                       (socketEx.ErrorCode == 10054 || socketEx.ErrorCode == 10053))
        {
            _isConnected = false;
        }
        catch (SocketException socketEx) when (socketEx.ErrorCode == 10054 || socketEx.ErrorCode == 10053)
        {
            _isConnected = false;
        }
        catch (Exception)
        {
            _isConnected = false;
        }
        finally
        {
            // Return rented buffer to pool
            ArrayPool<byte>.Shared.Return(lengthBuffer);
        }
    }

    private async Task ReceiveLoop()
    {
        // Rent buffer from ArrayPool instead of allocating new one
        byte[] buffer = ArrayPool<byte>.Shared.Rent(2 * 1024 * 1024);

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
                        return;
                    }

                    length |= ((uint)(b & 0x7F)) << shift;
                    shift += 7;

                    if ((b & 0x80) == 0)
                        break;

                    if (shift > 32)
                    {
                        return;
                    }
                }

                if (length > 1024 * 1024)
                {
                    break;
                }

                // Read payload
                await ReadExactAsync(buffer.AsMemory(0, (int)length), _cts.Token);
                _lastReceive = DateTime.UtcNow;

                // Parse command
                try
                {
                    var reader = new BinaryReader(buffer.AsSpan(0, (int)length).ToArray());
                    var command = ClientCommand.ReadBinary(reader);

                    // Handle command
                    await HandleCommandAsync(command);
                }
                catch (Exception)
                {
                    // Continue processing instead of terminating connection
                    continue;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (IOException ioEx) when (ioEx.InnerException is SocketException socketEx &&
                                       (socketEx.ErrorCode == 10054 || socketEx.ErrorCode == 10053))
        {
            _isConnected = false;
        }
        catch (SocketException socketEx) when (socketEx.ErrorCode == 10054 || socketEx.ErrorCode == 10053)
        {
            _isConnected = false;
        }
        catch (EndOfStreamException)
        {
            _isConnected = false;
        }
        catch (Exception)
        {
            _isConnected = false;
        }
        finally
        {
            // Return rented buffer to pool
            ArrayPool<byte>.Shared.Return(buffer);
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
        catch (Exception)
        {
            // Ignore handler errors to keep the connection alive
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
