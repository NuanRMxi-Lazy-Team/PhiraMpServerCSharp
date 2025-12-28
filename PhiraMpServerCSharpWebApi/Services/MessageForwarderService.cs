using Microsoft.Extensions.Hosting;
using PhiraMpServer.ExternalInterface.Common;
using PhiraMpServerCSharpWebApi.Services;

namespace PhiraMpServerCSharpWebApi.Services;

public class MessageForwarderService : IHostedService, IDisposable
{
    private readonly PhiraMpClientService _clientService;
    private readonly WebSocketManager _webSocketManager;
    private readonly ILogger<MessageForwarderService> _logger;
    private bool _disposed;

    public MessageForwarderService(
        PhiraMpClientService clientService,
        WebSocketManager webSocketManager,
        ILogger<MessageForwarderService> logger)
    {
        _clientService = clientService;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // 为所有已连接的客户端注册消息处理程序
        foreach (var (serverName, client) in _clientService.Clients)
        {
            RegisterMessageHandler(serverName, client);
        }

        // 监听新连接的客户端
        // 注意：这里需要在PhiraMpClientService中添加事件或轮询机制
        // 为简化实现，我们假定服务器连接在应用启动时已经建立
        
        return Task.CompletedTask;
    }

    private void RegisterMessageHandler(string serverName, PhiraMpServer.ExternalInterface.Client client)
    {
        client.OnServerMessageReceived += async (message) =>
        {
            try
            {
                switch (message)
                {
                    case RoomsChangedMessage roomsChanged:
                        await _webSocketManager.NotifyRoomsChangedAsync(serverName, roomsChanged.RoomList);
                        _logger.LogInformation("Forwarded RoomsChangedMessage for server '{ServerName}' with {Count} rooms", 
                            serverName, roomsChanged.RoomList.Length);
                        break;
                    
                    case RoomChangedMessage roomChanged:
                        await _webSocketManager.NotifyRoomChangedAsync(serverName, roomChanged.Room);
                        _logger.LogInformation("Forwarded RoomChangedMessage for server '{ServerName}', room '{RoomId}'", 
                            serverName, roomChanged.Room.RoomId);
                        break;
                    
                    default:
                        _logger.LogDebug("Received unknown server message type: {MessageType}", message.GetType().Name);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error forwarding server message for server '{ServerName}'", serverName);
            }
        };
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}