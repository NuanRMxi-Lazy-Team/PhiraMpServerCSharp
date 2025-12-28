using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace PhiraMpServerCSharpWebApi.Controllers;

[ApiController]
public class WebSocketController : ControllerBase
{
    private readonly Services.WebSocketManager _webSocketManager;
    private readonly ILogger<WebSocketController> _logger;

    public WebSocketController(
        Services.WebSocketManager webSocketManager,
        ILogger<WebSocketController> logger)
    {
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    [HttpGet("api/ws/{serverName}/rooms")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task GetServerRoomsWebSocket(string serverName)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        
        // 添加到服务器房间列表连接
        _webSocketManager.AddServerConnection(serverName, webSocket);
        
        _logger.LogInformation("WebSocket connected for server '{ServerName}' room list", serverName);

        try
        {
            // 保持连接打开
            await ReceiveLoop(webSocket, async (message) =>
            {
                // 对于房间列表WebSocket，我们主要只处理来自服务器的消息
                // 客户端通常不会发送消息到这个端点
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in server room WebSocket for '{ServerName}'", serverName);
        }
        finally
        {
            // 从服务器房间列表连接移除
            _webSocketManager.RemoveServerConnection(serverName, webSocket);
            _logger.LogInformation("WebSocket disconnected for server '{ServerName}' room list", serverName);
        }
    }
    
    [HttpGet("api/ws/{serverName}/{roomId}")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task GetRoomWebSocket(string serverName, string roomId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        
        // 添加到特定房间连接
        _webSocketManager.AddServerRoomConnection(serverName, roomId, webSocket);
        
        _logger.LogInformation("WebSocket connected for server '{ServerName}' room '{RoomId}'", serverName, roomId);

        try
        {
            // 保持连接打开
            await ReceiveLoop(webSocket, async (message) =>
            {
                // 对于房间WebSocket，我们主要只处理来自服务器的消息
                // 客户端通常不会发送消息到这个端点
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in room WebSocket for server '{ServerName}' room '{RoomId}'", serverName, roomId);
        }
        finally
        {
            // 从特定房间连接移除
            _webSocketManager.RemoveServerRoomConnection(serverName, roomId, webSocket);
            _logger.LogInformation("WebSocket disconnected for server '{ServerName}' room '{RoomId}'", serverName, roomId);
        }
    }

    private async Task ReceiveLoop(WebSocket webSocket, Func<string, Task> onMessageReceived)
    {
        var buffer = new byte[4096];
        
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                
                // 处理接收到的消息
                await onMessageReceived(message);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                break;
            }
        }
    }
    

}