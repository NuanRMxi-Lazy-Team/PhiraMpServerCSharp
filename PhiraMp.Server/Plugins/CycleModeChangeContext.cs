using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 循环模式变化事件上下文
/// </summary>
public class CycleModeChangeContext : BasePipelineContext
{
    /// <summary>房间对象</summary>
    public IRoom Room { get; }
    
    /// <summary>操作的用户</summary>
    public IUser User { get; }
    
    /// <summary>是否启用循环模式</summary>
    public bool CycleEnabled { get; }
    
    public CycleModeChangeContext(IRoom room, IUser user, bool cycleEnabled)
    {
        Room = room;
        User = user;
        CycleEnabled = cycleEnabled;
    }
}
