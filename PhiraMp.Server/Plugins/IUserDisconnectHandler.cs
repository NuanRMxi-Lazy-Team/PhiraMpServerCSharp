namespace PhiraMp.Server.Plugins;

/// <summary>
/// 用户断开连接处理器接口 - 用户重连超时后彻底离线时触发
/// 使用 [Export(typeof(IUserDisconnectHandler))] 注册
/// </summary>
public interface IUserDisconnectHandler
{
    Task HandleUserDisconnectAsync(UserDisconnectContext context);
}
