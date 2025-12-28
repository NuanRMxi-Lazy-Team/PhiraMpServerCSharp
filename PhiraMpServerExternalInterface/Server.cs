using System.Net.Sockets;
using ProtoBuf;
using PhiraMpServer.ExternalInterface.Common;
using System.Collections.Concurrent;

namespace PhiraMpServer.ExternalInterface;

public class Server : IDisposable
{
    private readonly int _port;
    private readonly string _ip;
    private readonly CancellationTokenSource _cts;
    private TcpListener _listener;
    private List<TcpClient> _clients = [];
    private readonly ConcurrentDictionary<TcpClient, bool> _authenticatedClients = new();
    private bool _disposed;
    public Action<string> OnInfo = s => { };
    public Action<string> OnError = s => { };
    public Action<string> OnWarning = s => { };
    private readonly ICommandDispatcher _commandDispatcher;
    
    public Server(string ip, int port, ICommandDispatcher? commandDispatcher = null)
    {
        _cts = new CancellationTokenSource();
        _ip = ip;
        _port = port;
        _commandDispatcher = commandDispatcher ?? new CommandDispatcher();
        var bindAddress = System.Net.IPAddress.Parse(_ip);
        _listener = new TcpListener(bindAddress, _port);
        if (bindAddress.AddressFamily == AddressFamily.InterNetworkV6)
            _listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
    }
    
    public async Task StartAsync()
    {
        _listener.Start();
        OnInfo.Invoke($"External Interface Server Listening on {_ip}:{_port}");
        _ = Task.Run(async () => await AcceptClientsAsync(), _cts.Token);
        await Task.CompletedTask;
    }

    private async Task AcceptClientsAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                lock (_clients)
                {
                    _clients.Add(client);
                }
                OnInfo.Invoke($"Client connected from {client.Client.RemoteEndPoint}");
                
                _ = Task.Run(async () => await HandleClientAsync(client), _cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            OnInfo.Invoke("Server stopped accepting new connections");
        }
        catch (Exception ex)
        {
            OnError.Invoke($"Error accepting clients: {ex.Message}");
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            var stream = client.GetStream();

            while (!_cts.Token.IsCancellationRequested && client.Connected)
            {
                // 读取消息长度（4字节）
                var lengthBuffer = new byte[4];
                var bytesRead = await ReadExactlyAsync(stream, lengthBuffer, 4, _cts.Token);
                
                if (bytesRead == 0)
                {
                    OnInfo.Invoke($"Client {client.Client.RemoteEndPoint} disconnected");
                    break;
                }

                var messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                
                if (messageLength is <= 0 or > 1048576) // 1MB 限制
                {
                    OnWarning.Invoke($"Invalid message length: {messageLength}");
                    break;
                }

                // 读取消息内容
                var messageBuffer = new byte[messageLength];
                bytesRead = await ReadExactlyAsync(stream, messageBuffer, messageLength, _cts.Token);
                
                if (bytesRead == 0)
                {
                    OnWarning.Invoke("Connection closed while reading message");
                    break;
                }

                await ProcessMessage(messageBuffer, client);
            }
        }
        catch (OperationCanceledException)
        {
            OnInfo.Invoke($"Client handler cancelled for {client.Client.RemoteEndPoint}");
        }
        catch (Exception ex)
        {
            OnError.Invoke($"Error handling client {client.Client.RemoteEndPoint}: {ex.Message}");
        }
        finally
        {
            RemoveClient(client);
            client.Close();
        }
    }

    private async Task<int> ReadExactlyAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken ct)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead, count - totalRead), ct);
            if (read == 0)
                return 0;
            totalRead += read;
        }
        return totalRead;
    }

    private async Task ProcessMessage(byte[] messageData, TcpClient client)
    {
        try
        {
            using var ms = new MemoryStream(messageData);
            var command = Serializer.Deserialize<Command>(ms);
            if (command != null)
            {
                OnInfo.Invoke($"Received command: {command.GetType()} from {client.Client.RemoteEndPoint}");

                // 未鉴权时只允许 AuthenticateCommand
                if (command is not AuthenticateCommand &&
                    (!_authenticatedClients.TryGetValue(client, out var authed) || !authed))
                {
                    OnWarning.Invoke($"Unauthorized command from {client.Client.RemoteEndPoint}");
                    await SendToClientAsync(client, new AuthenticateResponse
                    {
                        Token = command.Token,
                        IsSuccess = false,
                        Message = "Unauthorized. Please authenticate first."
                    });
                    return;
                }

                var result = await _commandDispatcher.DispatchAsync(command);
                
                // 鉴权命令结果：更新状态
                if (result is AuthenticateResponse authResp)
                {
                    _authenticatedClients.AddOrUpdate(client, authResp.IsSuccess, (_, _) => authResp.IsSuccess);
                }

                result.Token ??= command.Token;
                await SendToClientAsync(client, result);
            }
            else
            {
                OnWarning.Invoke("Failed to deserialize command");
            }
        }
        catch (Exception ex)
        {
            OnError.Invoke($"Protobuf deserialization error: {ex.Message}");
        }
    }

    private void RemoveClient(TcpClient client)
    {
        lock (_clients)
        {
            _clients.Remove(client);
        }
        _authenticatedClients.TryRemove(client, out _); // 清理鉴权状态
    }

    public async Task SendToClientAsync(TcpClient client, CommandResponse commandResponse)
    {
        try
        {
            if (client.Connected)
            {
                using var ms = new MemoryStream();
                Serializer.Serialize(ms, commandResponse);
                var data = ms.ToArray();
                var lengthPrefix = BitConverter.GetBytes(data.Length);
                
                await client.GetStream().WriteAsync(lengthPrefix, _cts.Token);
                await client.GetStream().WriteAsync(data, _cts.Token);
            }
        }
        catch (Exception ex)
        {
            OnError.Invoke($"Error sending to client: {ex.Message}");
        }
    }
    
    public async Task SendToClientAsync(TcpClient client, ServerMessages serverMessages)
    {
        try
        {
            if (client.Connected)
            {
                using var ms = new MemoryStream();
                Serializer.Serialize(ms, serverMessages);
                var data = ms.ToArray();
                var lengthPrefix = BitConverter.GetBytes(data.Length);
                
                await client.GetStream().WriteAsync(lengthPrefix, _cts.Token);
                await client.GetStream().WriteAsync(data, _cts.Token);
            }
        }
        catch (Exception ex)
        {
            OnError.Invoke($"Error sending to client: {ex.Message}");
        }
    }

    public async Task BroadcastAsync(ServerMessages serverMessages)
    {
        // 只广播给已鉴权客户端
        List<TcpClient> clientsCopy;
        lock (_clients)
        {
            clientsCopy = new List<TcpClient>(_clients);
        }
        foreach (var client in clientsCopy)
        {
            if (_authenticatedClients.TryGetValue(client, out var authed) && authed)
            {
                await SendToClientAsync(client, serverMessages);
            }
        }
    }

    public async Task BroadcastAsync(CommandResponse commandResponse)
    {
        List<TcpClient> clientsCopy;
        lock (_clients)
        {
            clientsCopy = new List<TcpClient>(_clients);
        }

        foreach (var client in clientsCopy)
        {
            await SendToClientAsync(client, commandResponse);
        }
    }

    public void Stop()
    {
        if (!_disposed)
        {
            _cts.Cancel();
            _listener.Stop();
            OnInfo.Invoke("Server stopped");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        Stop();

        lock (_clients)
        {
            foreach (var client in _clients)
            {
                client.Close();
            }
            _clients.Clear();
        }

        _cts.Dispose();
        _disposed = true;
    }
}