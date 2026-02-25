using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 加入房间请求事件上下文
/// </summary>
public class JoinRoomRequestContext : BasePipelineContext
{
    /// <summary>发起请求的用户</summary>
    public IUser User { get; }
    
    /// <summary>房间 ID</summary>
    public RoomId RoomId { get; }
    
    /// <summary>是否为监视模式</summary>
    public bool Monitor { get; }
    
    public JoinRoomRequestContext(IUser user, RoomId roomId, bool monitor)
    {
        User = user;
        RoomId = roomId;
        Monitor = monitor;
    }
}
