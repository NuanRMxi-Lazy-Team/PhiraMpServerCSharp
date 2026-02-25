namespace PhiraMp.Server.Plugins;

/// <summary>
/// 加入房间请求处理器接口 - 导出此接口以拦截加入房间请求
/// 使用 [Export(typeof(IJoinRoomRequestHandler))] 注册
/// 插件可以修改目标房间 ID 或抛出异常来阻止加入
/// </summary>
public interface IJoinRoomRequestHandler
{
    Task HandleJoinRoomRequestAsync(JoinRoomRequestContext context);
}
