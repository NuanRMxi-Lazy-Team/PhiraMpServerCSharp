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
    IEnumerable<IRoom> GetAllRooms();
    
    /// <summary>
    /// 根据 ID 获取房间
    /// </summary>
    IRoom? GetRoom(string roomId);
    
    /// <summary>
    /// 锁定或解锁房间
    /// </summary>
    Task SetRoomLockAsync(IRoom room, bool locked);
    
    /// <summary>
    /// 设置房间循环模式
    /// </summary>
    Task SetRoomCycleAsync(IRoom room, bool cycle);
    
    /// <summary>
    /// 关闭房间（踢出所有玩家）
    /// </summary>
    Task CloseRoomAsync(IRoom room, string reason = "房间已关闭");
    
    // ===== 用户管理 =====
    
    /// <summary>
    /// 获取所有在线用户
    /// </summary>
    IEnumerable<IUser> GetAllUsers();
    
    /// <summary>
    /// 根据 ID 获取用户
    /// </summary>
    IUser? GetUser(int userId);
    
    /// <summary>
    /// 将用户从房间移除
    /// </summary>
    Task RemoveUserFromRoomAsync(IUser user, string reason = "被移出房间");
    
    // ===== 消息发送 =====
    
    /// <summary>
    /// 向房间发送聊天消息
    /// </summary>
    Task SendRoomMessageAsync(IRoom room, string message, int senderId = -1);
    
    /// <summary>
    /// 向用户发送私聊消息
    /// </summary>
    Task SendPrivateMessageAsync(IUser user, string message, int senderId = -1);
    
    /// <summary>
    /// 向所有在线用户广播消息
    /// </summary>
    Task BroadcastMessageAsync(string message, int senderId = -1);
    
    // ===== 发送任意命令的返回 =====
    Task SendCommandAsync(IUser user, ServerCommand cmd);
    
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

