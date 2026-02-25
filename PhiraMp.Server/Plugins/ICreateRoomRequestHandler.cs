namespace PhiraMp.Server.Plugins;

/// <summary>
/// 创建房间请求处理器接口 - 导出此接口以拦截创建房间请求
/// 使用 [Export(typeof(ICreateRoomRequestHandler))] 注册
/// 插件可以抛出异常来阻止房间创建
/// </summary>
public interface ICreateRoomRequestHandler
{
    Task HandleCreateRoomRequestAsync(CreateRoomRequestContext context);
}
