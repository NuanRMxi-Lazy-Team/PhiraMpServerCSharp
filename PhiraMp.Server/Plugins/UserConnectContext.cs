using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 用户连接事件上下文 - 用户完成鉴权后触发
/// </summary>
public class UserConnectContext : BasePipelineContext
{
    /// <summary>连接的用户对象</summary>
    public IUser User { get; }

    /// <summary>当前会话 ID</summary>
    public Guid SessionId { get; }

    /// <summary>是否为重新连接（true = 断线重连，false = 首次连接）</summary>
    public bool IsReconnect { get; }

    public UserConnectContext(IUser user, Guid sessionId, bool isReconnect)
    {
        User = user;
        SessionId = sessionId;
        IsReconnect = isReconnect;
    }
}
