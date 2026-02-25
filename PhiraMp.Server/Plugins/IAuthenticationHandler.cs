namespace PhiraMp.Server.Plugins;

/// <summary>
/// 鉴权处理器接口 - 导出此接口以拦截鉴权流程
/// 使用 [Export(typeof(IAuthenticationHandler))] 注册
/// 插件可以验证、修改用户信息或抛出异常来阻止鉴权
/// </summary>
public interface IAuthenticationHandler
{
    Task HandleAuthenticationAsync(AuthenticationContext context);
}
