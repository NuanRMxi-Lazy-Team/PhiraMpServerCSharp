namespace PhiraMp.Server.Plugins;

/// <summary>
/// 管线处理器接口 - 泛型接口，支持不同类型的上下文
/// </summary>
/// <typeparam name="TContext">上下文类型</typeparam>
public interface IPipelineHandler<in TContext> where TContext : IPipelineContext
{
    /// <summary>
    /// 处理器优先级（数字越小优先级越高，优先执行）
    /// 默认优先级为 100
    /// </summary>
    int Priority => 100;
    
    /// <summary>
    /// 处理管线上下文
    /// </summary>
    Task HandleAsync(TContext context);
}
