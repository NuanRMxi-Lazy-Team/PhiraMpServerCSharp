using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 创建房间请求事件上下文
/// </summary>
public class CreateRoomRequestContext : BasePipelineContext
{
    /// <summary>发起请求的用户</summary>
    public IUser User { get; }
    
    /// <summary>请求创建的房间 ID</summary>
    public RoomId RoomId { get; }
    
    public CreateRoomRequestContext(IUser user, RoomId roomId)
    {
        User = user;
        RoomId = roomId;
    }
}
