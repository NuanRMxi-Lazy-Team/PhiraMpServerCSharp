namespace PhiraMp.Server.Plugins;

/// <summary>
/// 用户加入处理器接口 - 导出此接口以处理用户加入
/// 使用 [Export(typeof(IUserJoinHandler))] 注册
/// </summary>
public interface IUserJoinHandler
{
    Task HandleUserJoinAsync(UserEventContext context);
}
