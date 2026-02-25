namespace PhiraMp.Server.Plugins;

/// <summary>
/// 用户离开处理器接口 - 导出此接口以处理用户离开
/// 使用 [Export(typeof(IUserLeaveHandler))] 注册
/// </summary>
public interface IUserLeaveHandler
{
    Task HandleUserLeaveAsync(UserEventContext context);
}
