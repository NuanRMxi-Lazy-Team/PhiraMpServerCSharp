namespace PhiraMp.Server.Plugins;

/// <summary>
/// 优先级接口 - 插件处理器可以实现此接口来指定优先级
/// </summary>
public interface IPrioritizedHandler
{
    /// <summary>
    /// 处理器优先级（数字越小优先级越高）
    /// </summary>
    int Priority { get; }
}
