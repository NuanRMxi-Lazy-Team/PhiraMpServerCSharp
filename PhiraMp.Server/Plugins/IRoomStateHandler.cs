namespace PhiraMp.Server.Plugins;

/// <summary>
/// 房间状态变化处理器接口 - 导出此接口以处理状态变化
/// 使用 [Export(typeof(IRoomStateHandler))] 注册
/// </summary>
public interface IRoomStateHandler
{
    Task HandleStateChangeAsync(RoomStateContext context);
}
