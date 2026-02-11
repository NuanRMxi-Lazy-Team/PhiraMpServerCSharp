using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 插件 API - 为插件提供对服务端的完整控制接口
/// </summary>
public interface IPluginAPI
{
    // ===== 房间管理 =====
    
    /// <summary>
    /// 获取所有活跃房间
    /// </summary>
    IEnumerable<Room> GetAllRooms();
    
    /// <summary>
    /// 根据 ID 获取房间
    /// </summary>
    Room? GetRoom(string roomId);
    
    /// <summary>
    /// 锁定或解锁房间
    /// </summary>
    Task SetRoomLockAsync(Room room, bool locked);
    
    /// <summary>
    /// 设置房间循环模式
    /// </summary>
    Task SetRoomCycleAsync(Room room, bool cycle);
    
    /// <summary>
    /// 关闭房间（踢出所有玩家）
    /// </summary>
    Task CloseRoomAsync(Room room, string reason = "房间已关闭");
    
    // ===== 用户管理 =====
    
    /// <summary>
    /// 获取所有在线用户
    /// </summary>
    IEnumerable<User> GetAllUsers();
    
    /// <summary>
    /// 根据 ID 获取用户
    /// </summary>
    User? GetUser(int userId);
    
    /// <summary>
    /// 将用户从房间移除
    /// </summary>
    Task RemoveUserFromRoomAsync(User user, string reason = "被移出房间");
    
    // ===== 消息发送 =====
    
    /// <summary>
    /// 向房间发送聊天消息
    /// </summary>
    Task SendRoomMessageAsync(Room room, string message, int senderId = -1);
    
    /// <summary>
    /// 向用户发送私聊消息
    /// </summary>
    Task SendPrivateMessageAsync(User user, string message, int senderId = -1);
    
    /// <summary>
    /// 向所有在线用户广播消息
    /// </summary>
    Task BroadcastMessageAsync(string message, int senderId = -1);
    
    // ===== 发送任意命令的返回 =====
    Task SendCommandAsync(User user, ServerCommand cmd);
    
    // ===== 服务器状态 =====
    
    /// <summary>
    /// 获取服务器状态
    /// </summary>
    ServerState GetServerState();
    
    /// <summary>
    /// 获取服务器配置
    /// </summary>
    ServerConfig GetServerConfig();
    
    /// <summary>
    /// 获取当前在线人数
    /// </summary>
    int GetOnlineUserCount();
    
    /// <summary>
    /// 获取当前活跃房间数
    /// </summary>
    int GetActiveRoomCount();
}

/// <summary>
/// 插件 API 实现
/// </summary>
public class PluginAPI : IPluginAPI
{
    private readonly ServerState _serverState;
    private readonly IPluginLogger _logger;

    public PluginAPI(ServerState serverState, IPluginLogger logger)
    {
        _serverState = serverState;
        _logger = logger;
    }

    // ===== 房间管理实现 =====
    
    public IEnumerable<Room> GetAllRooms()
    {
        return _serverState.Rooms.Values;
    }

    public Room? GetRoom(string roomId)
    {
        _serverState.Rooms.TryGetValue(roomId, out var room);
        return room;
    }

    public async Task SetRoomLockAsync(Room room, bool locked)
    {
        room.Locked = locked;
        await room.SendAsync(new LockRoomMessage(locked));
        _logger.Debug($"房间 {room.Id} 锁定状态设置为: {locked}");
    }

    public async Task SetRoomCycleAsync(Room room, bool cycle)
    {
        room.SetCycle(cycle);
        await room.SendAsync(new CycleRoomMessage(cycle));
        _logger.Debug($"房间 {room.Id} 循环模式设置为: {cycle}");
    }

    public async Task CloseRoomAsync(Room room, string reason = "房间已关闭")
    {
        var users = room.GetAllUsers().ToList();
        foreach (var user in users)
        {
            await RemoveUserFromRoomAsync(user, reason);
        }
        _logger.Info($"已关闭房间 {room.Id}");
    }

    // ===== 用户管理实现 =====
    
    public IEnumerable<User> GetAllUsers()
    {
        return _serverState.Users.Values;
    }

    public User? GetUser(int userId)
    {
        _serverState.Users.TryGetValue(userId, out var user);
        return user;
    }

    public async Task RemoveUserFromRoomAsync(User user, string reason = "被移出房间")
    {
        if (user.Room != null)
        {
            _logger.Info($"将用户 {user.Name} 从房间 {user.Room.Id} 移除: {reason}");
            await SendPrivateMessageAsync(user, reason);
            
            var room = user.Room;
            // 使用OnUserLeaveAsync来正确移除用户
            await room.OnUserLeaveAsync(user);
            user.Room = null;
        }
    }

    // ===== 消息发送实现 =====
    
    public async Task SendRoomMessageAsync(Room room, string message, int senderId = -1)
    {
        await room.SendAsync(new ChatMessage(senderId, message));
        _logger.Debug($"向房间 {room.Id} 发送消息: {message}");
    }

    public async Task SendPrivateMessageAsync(User user, string message, int senderId = -1)
    {
        await user.TrySendAsync(new MessageCommand(new ChatMessage(senderId, message)));
        _logger.Debug($"向用户 {user.Name} 发送私聊: {message}");
    }

    public async Task BroadcastMessageAsync(string message, int senderId = -1)
    {
        var users = GetAllUsers().ToList();
        var cmd = new MessageCommand(new ChatMessage(senderId, message));
        foreach (var user in users)
        {
            await user.TrySendAsync(cmd);
        }
        _logger.Info($"广播消息给 {users.Count} 个用户: {message}");
    }
    
    // ==== 发送任意命令实现 =====
    public async Task SendCommandAsync(User user, ServerCommand cmd)
    {
        await user.TrySendAsync(cmd);
        _logger.Debug($"向用户 {user.Name} 发送命令: {cmd.GetType().Name}");
    }

    // ===== 服务器状态实现 =====
    
    public ServerState GetServerState()
    {
        return _serverState;
    }

    public ServerConfig GetServerConfig()
    {
        return _serverState.Config;
    }

    public int GetOnlineUserCount()
    {
        return GetAllUsers().Count();
    }

    public int GetActiveRoomCount()
    {
        return GetAllRooms().Count();
    }
}
