using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 请求开始游戏事件上下文
/// </summary>
public class RequestStartContext : BasePipelineContext
{
    /// <summary>房间对象</summary>
    public IRoom Room { get; }
    
    /// <summary>发起请求的用户</summary>
    public IUser User { get; }
    
    public RequestStartContext(IRoom room, IUser user)
    {
        Room = room;
        User = user;
    }
}
