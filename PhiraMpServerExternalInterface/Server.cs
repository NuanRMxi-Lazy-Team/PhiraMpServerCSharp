using System.Net.Sockets;
using ProtoBuf;
using PhiraMpServer.ExternalInterface.Common;

namespace PhiraMpServer.ExternalInterface;

public class Server : IDisposable
{
    private readonly int _port;
    private readonly string _ip;
    private readonly CancellationTokenSource _cts;
    private TcpListener _listener;
    private List<TcpClient> _clients = [];
    private bool _disposed;
    public Action<string> OnInfo = s => { };
    public Action<string> OnError = s => { };
    public Action<string> OnWarning = s => { };
    public Func<Command, Task<CommandResponse>> OnCommandReceived = cmd => Task.FromResult<CommandResponse>(default);
    public Server(string ip, int port)
    {
        _cts = new CancellationTokenSource();
        _ip = ip;
        _port = port;
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
                
                if (messageLength <= 0 || messageLength > 1048576) // 1MB 限制
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
                var result = await OnCommandReceived.Invoke(command);
                if (result != null)
                {
                    // 让响应携带请求的 Token
                    result.Token ??= command.Token;
                    await SendToClientAsync(client, result as CommandResponse);
                }
                else
                {
                    OnWarning.Invoke("OnCommandReceived returned null response");
                }
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