using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 用户加入/离开事件上下文
/// </summary>
public class UserEventContext : BasePipelineContext
{
    /// <summary>房间对象</summary>
    public IRoom Room { get; }
    
    /// <summary>用户对象</summary>
    public IUser User { get; }
    
    public UserEventContext(IRoom room, IUser user)
    {
        Room = room;
        User = user;
    }
}
