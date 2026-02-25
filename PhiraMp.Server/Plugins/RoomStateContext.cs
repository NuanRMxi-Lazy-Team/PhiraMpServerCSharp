using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 房间状态变化事件上下文
/// </summary>
public class RoomStateContext : BasePipelineContext
{
    /// <summary>房间对象</summary>
    public IRoom Room { get; }
    
    /// <summary>新状态</summary>
    public string NewState { get; }
    
    public RoomStateContext(IRoom room, string newState)
    {
        Room = room;
        NewState = newState;
    }
}
