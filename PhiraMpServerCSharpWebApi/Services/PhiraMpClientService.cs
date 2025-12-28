using Microsoft.Extensions.Options;
using PhiraMpServer.ExternalInterface;
using PhiraMpServerCSharpWebApi.Configuration;
using System.Collections.Concurrent;

namespace PhiraMpServerCSharpWebApi.Services;

public class PhiraMpClientService : IHostedService, IDisposable
{
    private readonly ILogger<PhiraMpClientService> _logger;
    private readonly List<PhiraMpServerOption> _serverOptions;
    private readonly ConcurrentDictionary<string, Client> _clients = new();
    private bool _disposed;

    public IReadOnlyDictionary<string, Client> Clients => _clients;

    public PhiraMpClientService(
        ILogger<PhiraMpClientService> logger,
        IOptions<PhiraMpServersOptions> options)
    {
        _logger = logger;
        _serverOptions = options.Value.Servers.Where(s => s.Enabled).ToList();
    }

    /// <summary>
    /// 获取指定名称的客户端
    /// </summary>
    public Client? GetClient(string serverName)
    {
        return _clients.TryGetValue(serverName, out var client) ? client : null;
    }

    /// <summary>
    /// 获取第一个可用的客户端
    /// </summary>
    public Client? GetFirstClient()
    {
        return _clients.Values.FirstOrDefault(c => c.IsConnected);
    }

    /// <summary>
    /// 获取所有已连接的客户端
    /// </summary>
    public IEnumerable<Client> GetConnectedClients()
    {
        return _clients.Values.Where(c => c.IsConnected);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting PhiraMp Client Service for {Count} servers...", _serverOptions.Count);
        
        var tasks = new List<Task>();

        foreach (var serverOption in _serverOptions)
        {
            tasks.Add(ConnectToServerAsync(serverOption, cancellationToken));
        }

        await Task.WhenAll(tasks);
        
        var connectedCount = _clients.Values.Count(c => c.IsConnected);
        _logger.LogInformation("PhiraMp Client Service started. {Connected}/{Total} servers connected.", 
            connectedCount, _serverOptions.Count);
    }

    private async Task ConnectToServerAsync(PhiraMpServerOption serverOption, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Connecting to server '{Name}' at {Host}:{Port}...", 
                serverOption.Name, serverOption.Host, serverOption.Port);

            var tokenSha256 = serverOption.GetTokenSha256();
            var client = new Client(serverOption.Host, serverOption.Port, tokenSha256);
            
            // 设置事件回调
            client.OnInfo = msg => _logger.LogInformation("[{ServerName}] {Message}", serverOption.Name, msg);
            client.OnError = msg => _logger.LogError("[{ServerName}] {Message}", serverOption.Name, msg);
            client.OnWarning = msg => _logger.LogWarning("[{ServerName}] {Message}", serverOption.Name, msg);
            client.OnResponseReceived = response => 
                _logger.LogDebug("[{ServerName}] Response received with token: {Token}", serverOption.Name, response.Token);

            await client.ConnectAsync();
            
            if (_clients.TryAdd(serverOption.Name, client))
            {
                _logger.LogInformation("Successfully connected to server '{Name}' at {Host}:{Port}", 
                    serverOption.Name, serverOption.Host, serverOption.Port);
            }
            else
            {
                _logger.LogWarning("Failed to add client '{Name}' to dictionary", serverOption.Name);
                client.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to server '{Name}' at {Host}:{Port}", 
                serverOption.Name, serverOption.Host, serverOption.Port);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping PhiraMp Client Service...");
        
        foreach (var (name, client) in _clients)
        {
            try
            {
                client.Disconnect();
                _logger.LogInformation("Disconnected from server '{Name}'", name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from server '{Name}'", name);
            }
        }
        
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        foreach (var (name, client) in _clients)
        {
            try
            {
                client.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing client '{Name}'", name);
            }
        }
        
        _clients.Clear();
        _disposed = true;
    }
}

