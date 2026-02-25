using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 房间消息事件上下文
/// </summary>
public class RoomMessageContext : BasePipelineContext
{
    /// <summary>房间对象</summary>
    public IRoom Room { get; }
    
    /// <summary>发送消息的用户</summary>
    public IUser User { get; }
    
    /// <summary>消息内容</summary>
    public string Message { get; }
    
    public RoomMessageContext(IRoom room, IUser user, string message)
    {
        Room = room;
        User = user;
        Message = message;
    }
}
