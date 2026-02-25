namespace PhiraMp.Server.Plugins;

/// <summary>
/// 请求开始游戏处理器接口 - 导出此接口以验证/修改开始请求
/// 使用 [Export(typeof(IRequestStartHandler))] 注册
/// 插件可以抛出异常来阻止游戏开始
/// </summary>
public interface IRequestStartHandler
{
    Task HandleRequestStartAsync(RequestStartContext context);
}
