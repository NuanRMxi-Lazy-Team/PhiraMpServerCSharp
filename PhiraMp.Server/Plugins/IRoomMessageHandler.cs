namespace PhiraMp.Server.Plugins;

/// <summary>
/// 房间消息处理器接口 - 导出此接口以处理房间消息
/// 使用 [Export(typeof(IRoomMessageHandler))] 注册
/// </summary>
public interface IRoomMessageHandler
{
    Task HandleMessageAsync(RoomMessageContext context);
}
