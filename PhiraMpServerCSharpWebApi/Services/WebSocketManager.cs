using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using PhiraMpServer.ExternalInterface.Common;
using PhiraMpServer.ExternalInterface.Model;

namespace PhiraMpServerCSharpWebApi.Services;

public class WebSocketManager
{
    // 存储服务器名称到WebSocket连接的映射
    private readonly ConcurrentDictionary<string, HashSet<WebSocket>> _serverRoomConnections = new();
    private readonly ConcurrentDictionary<string, HashSet<WebSocket>> _serverConnections = new();
    
    public void AddServerRoomConnection(string serverName, string roomId, WebSocket webSocket)
    {
        var key = $"{serverName}_{roomId}";
        if (!_serverRoomConnections.TryGetValue(key, out var connections))
        {
            connections = new HashSet<WebSocket>();
            _serverRoomConnections[key] = connections;
        }
        
        connections.Add(webSocket);
    }
    
    public void RemoveServerRoomConnection(string serverName, string roomId, WebSocket webSocket)
    {
        var key = $"{serverName}_{roomId}";
        if (_serverRoomConnections.TryGetValue(key, out var connections))
        {
            connections.Remove(webSocket);
            if (connections.Count == 0)
            {
                _serverRoomConnections.TryRemove(key, out _);
            }
        }
    }
    
    public void AddServerConnection(string serverName, WebSocket webSocket)
    {
        if (!_serverConnections.TryGetValue(serverName, out var connections))
        {
            connections = new HashSet<WebSocket>();
            _serverConnections[serverName] = connections;
        }
        
        connections.Add(webSocket);
    }
    
    public void RemoveServerConnection(string serverName, WebSocket webSocket)
    {
        if (_serverConnections.TryGetValue(serverName, out var connections))
        {
            connections.Remove(webSocket);
            if (connections.Count == 0)
            {
                _serverConnections.TryRemove(serverName, out _);
            }
        }
    }

    public async Task NotifyRoomsChangedAsync(string serverName, string[] roomList)
    {
        var key = serverName;
        if (_serverConnections.TryGetValue(key, out var connections))
        {
            var message = new
            {
                type = "roomsChanged",
                rooms = roomList
            };
            
            var json = JsonSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(json);
            
            var tasks = new List<Task>();
            foreach (var connection in connections)
            {
                if (connection.State == WebSocketState.Open)
                {
                    tasks.Add(connection.SendAsync(
                        new ArraySegment<byte>(buffer, 0, buffer.Length),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None));
                }
            }
            
            await Task.WhenAll(tasks);
        }
    }
    
    public async Task NotifyRoomChangedAsync(string serverName, RoomRecord roomRecord)
    {
        // 通知特定房间的连接
        var roomKey = $"{serverName}_{roomRecord.RoomId}";
        if (_serverRoomConnections.TryGetValue(roomKey, out var roomConnections))
        {
            var message = new
            {
                type = "roomChanged",
                room = roomRecord
            };
            
            var json = JsonSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(json);
            
            var tasks = new List<Task>();
            foreach (var connection in roomConnections)
            {
                if (connection.State == WebSocketState.Open)
                {
                    tasks.Add(connection.SendAsync(
                        new ArraySegment<byte>(buffer, 0, buffer.Length),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None));
                }
            }
            
            await Task.WhenAll(tasks);
        }
    }
}