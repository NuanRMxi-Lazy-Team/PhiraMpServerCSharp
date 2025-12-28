using System.Collections.Concurrent;
using System.Net.Sockets;
using ProtoBuf;
using PhiraMpServer.ExternalInterface.Common;

namespace PhiraMpServer.ExternalInterface;

public class Client : IDisposable
{
    private readonly string _ip;
    private readonly int _port;
    private readonly string? _tokenSha256;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;
    private bool _isConnected;
    private bool _isAuthenticated;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<CommandResponse>> _pendingResponses = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public Action<string> OnInfo = s => { };
    public Action<string> OnError = s => { };
    public Action<string> OnWarning = s => { };
    public Action<CommandResponse> OnResponseReceived = _ => { };
    public Action<ServerMessages> OnServerMessageReceived = _ => { };
    public bool IsConnected => _isConnected && _client?.Connected == true;
    public bool IsAuthenticated => _isAuthenticated;

    public Client(string ip, int port, string? tokenSha256 = null)
    {
        _ip = ip;
        _port = port;
        _tokenSha256 = tokenSha256;
    }

    public async Task ConnectAsync()
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_ip, _port, _cts.Token);
            _stream = _client.GetStream();
            _isConnected = true;
            OnInfo.Invoke($"Connected to {_ip}:{_port}");
            _ = Task.Run(ReceiveLoopAsync, _cts.Token);

            // 如果提供了 Token，自动进行认证
            if (!string.IsNullOrEmpty(_tokenSha256))
            {
                await AuthenticateAsync(_tokenSha256);
            }
            else
            {
                OnWarning.Invoke("No token provided, authentication skipped");
                _isAuthenticated = true; // 如果没有 token，假定不需要认证
            }
        }
        catch (Exception ex)
        {
            _isConnected = false;
            OnError.Invoke($"Connect failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 进行认证
    /// </summary>
    private async Task AuthenticateAsync(string tokenSha256)
    {
        try
        {
            OnInfo.Invoke("Authenticating...");
            var authCommand = new AuthenticateCommand
            {
                TokenSha256 = tokenSha256
            };

            var response = await SendCommandAndWaitAsync(authCommand, timeoutMs: 5000);

            if (response is AuthenticateResponse authResponse)
            {
                _isAuthenticated = authResponse.IsSuccess;
                if (authResponse.IsSuccess)
                {
                    OnInfo.Invoke("Authentication successful");
                }
                else
                {
                    OnError.Invoke($"Authentication failed: {authResponse.Message}");
                    throw new InvalidOperationException($"Authentication failed: {authResponse.Message}");
                }
            }
            else
            {
                OnError.Invoke("Unexpected authentication response");
                throw new InvalidOperationException("Unexpected authentication response");
            }
        }
        catch (Exception ex)
        {
            _isAuthenticated = false;
            OnError.Invoke($"Authentication error: {ex.Message}");
            throw;
        }
    }

    public async Task SendCommandAsync(Command command)
    {
        if (!IsConnected) throw new InvalidOperationException("Not connected");
        try
        {
            var token = EnsureToken(command);
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, command);
            var data = ms.ToArray();
            var len = BitConverter.GetBytes(data.Length);
            await _stream!.WriteAsync(len, _cts.Token);
            await _stream.WriteAsync(data, _cts.Token);
            OnInfo.Invoke($"Sent command: {command.GetType()} (token: {token})");
        }
        catch (Exception ex)
        {
            OnError.Invoke($"Send error: {ex.Message}");
            throw;
        }
    }

    public async Task<CommandResponse> SendCommandAndWaitAsync(Command command, int timeoutMs = 10000)
    {
        if (!IsConnected) throw new InvalidOperationException("Not connected");
        var token = EnsureToken(command);
        var tcs = new TaskCompletionSource<CommandResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_pendingResponses.TryAdd(token, tcs))
            throw new InvalidOperationException($"Token already exists: {token}");

        try
        {
            await SendCommandAsync(command);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            linked.CancelAfter(timeoutMs);
            await using (linked.Token.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }
        finally
        {
            _pendingResponses.TryRemove(token, out _);
        }
    }

    private string EnsureToken(Command command)
    {
        if (string.IsNullOrWhiteSpace(command.Token))
            command.Token = Guid.NewGuid().ToString("N");
        return command.Token;
    }

    private async Task ReceiveLoopAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested && IsConnected)
            {
                var lenBuf = new byte[4];
                var read = await ReadExactlyAsync(_stream!, lenBuf, 4, _cts.Token);
                if (read == 0)
                {
                    OnInfo.Invoke("Server disconnected");
                    _isConnected = false;
                    break;
                }

                var msgLen = BitConverter.ToInt32(lenBuf, 0);
                if (msgLen is <= 0 or > 1048576)
                {
                    OnWarning.Invoke($"Invalid length: {msgLen}");
                    break;
                }

                var buf = new byte[msgLen];
                read = await ReadExactlyAsync(_stream!, buf, msgLen, _cts.Token);
                if (read == 0)
                {
                    OnWarning.Invoke("Closed while reading");
                    _isConnected = false;
                    break;
                }

                await ProcessResponseAsync(buf);
            }
        }
        catch (OperationCanceledException)
        {
            OnInfo.Invoke("Receive loop cancelled");
        }
        catch (Exception ex)
        {
            OnError.Invoke($"Receive error: {ex.Message}");
            _isConnected = false;
            foreach (var kv in _pendingResponses) kv.Value.TrySetException(ex);
        }
    }

    private async Task<int> ReadExactlyAsync(NetworkStream stream, byte[] buffer, int count, CancellationToken ct)
    {
        var total = 0;
        while (total < count)
        {
            var r = await stream.ReadAsync(buffer.AsMemory(total, count - total), ct);
            if (r == 0) return 0;
            total += r;
        }

        return total;
    }

    private async Task ProcessResponseAsync(byte[] data)
    {
        try
        {
            using var ms = new MemoryStream(data);

            // 尝试反序列化为 CommandResponse
            try
            {
                var resp = Serializer.Deserialize<CommandResponse>(ms);
                if (resp != null)
                {
                    OnInfo.Invoke($"Received response for {resp.GetType()} (token: {resp.Token})");
                    OnResponseReceived.Invoke(resp);

                    if (!string.IsNullOrWhiteSpace(resp.Token) && _pendingResponses.TryRemove(resp.Token, out var tcs))
                        tcs.TrySetResult(resp);
                    else
                        OnWarning.Invoke($"No pending request for token {resp.Token}");
                    return;
                }
            }
            catch
            {
                // 如果不是 CommandResponse，尝试反序列化为 ServerMessages
                ms.Position = 0;
            }

            // 尝试反序列化为 ServerMessages
            var serverMsg = Serializer.Deserialize<ServerMessages>(ms);
            if (serverMsg != null)
            {
                OnInfo.Invoke($"Received server message: {serverMsg.GetType()}");
                OnServerMessageReceived.Invoke(serverMsg);
            }
            else
            {
                OnWarning.Invoke("Deserialize failed for both CommandResponse and ServerMessages");
            }
        }
        catch (Exception ex)
        {
            OnError.Invoke($"Deserialize error: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    public void Disconnect()
    {
        if (_disposed) return;
        _cts.Cancel();
        _isConnected = false;
        _stream?.Close();
        _client?.Close();
        foreach (var kv in _pendingResponses) kv.Value.TrySetCanceled();
        _pendingResponses.Clear();
        OnInfo.Invoke("Disconnected");
    }

    public void Dispose()
    {
        if (_disposed) return;
        Disconnect();
        _stream?.Dispose();
        _client?.Dispose();
        _cts.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}