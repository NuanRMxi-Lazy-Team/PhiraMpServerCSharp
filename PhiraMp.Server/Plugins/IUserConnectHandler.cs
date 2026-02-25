namespace PhiraMp.Server.Plugins;

/// <summary>
/// 用户连接处理器接口 - 用户完成鉴权后正式加入服务器时触发（含重连）
/// 使用 [Export(typeof(IUserConnectHandler))] 注册
/// 插件可以抛出异常来拒绝/踢出用户
/// </summary>
public interface IUserConnectHandler
{
    Task HandleUserConnectAsync(UserConnectContext context);
}
