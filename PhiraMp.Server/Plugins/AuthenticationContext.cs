using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 鉴权事件上下文
/// </summary>
public class AuthenticationContext : BasePipelineContext
{
    /// <summary>鉴权令牌</summary>
    public string Token { get; }
    
    /// <summary>用户信息（从 Phira 服务器获取，插件可以修改）</summary>
    public PhiraUserInfo UserInfo { get; set; }
    
    /// <summary>会话 ID</summary>
    public Guid SessionId { get; }
    
    public AuthenticationContext(string token, PhiraUserInfo userInfo, Guid sessionId)
    {
        Token = token;
        UserInfo = userInfo;
        SessionId = sessionId;
    }
}
