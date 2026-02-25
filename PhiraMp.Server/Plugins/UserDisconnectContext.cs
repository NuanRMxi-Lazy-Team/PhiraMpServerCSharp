using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 用户断开连接事件上下文 - 重连超时后彻底离线时触发
/// </summary>
public class UserDisconnectContext : BasePipelineContext
{
    /// <summary>断开连接的用户对象</summary>
    public IUser User { get; }

    public UserDisconnectContext(IUser user)
    {
        User = user;
    }
}
