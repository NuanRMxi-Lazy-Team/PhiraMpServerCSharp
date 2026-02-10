namespace PhiraMp.Server.Plugins;

/// <summary>
/// 管线上下文接口 - 所有管线处理器的上下文都应该实现此接口
/// 支持提前返回和结果传递
/// </summary>
public interface IPipelineContext
{
    /// <summary>
    /// 标记此请求是否已被处理（已处理则停止管线）
    /// </summary>
    bool IsHandled { get; set; }
    
    /// <summary>
    /// 处理结果（可选）
    /// </summary>
    object? Result { get; set; }
}

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

/// <summary>
/// 管线执行器 - 负责按优先级顺序执行处理器
/// </summary>
public class PipelineExecutor
{
    /// <summary>
    /// 执行管线处理器
    /// </summary>
    /// <typeparam name="TContext">上下文类型</typeparam>
    /// <param name="handlers">处理器列表</param>
    /// <param name="context">上下文对象</param>
    public static async Task ExecuteAsync<TContext>(
        IEnumerable<IPipelineHandler<TContext>> handlers, 
        TContext context) 
        where TContext : IPipelineContext
    {
        // 按优先级排序（数字越小优先级越高）
        var sortedHandlers = handlers
            .OrderBy(h => h.Priority)
            .ToList();
        
        foreach (var handler in sortedHandlers)
        {
            try
            {
                await handler.HandleAsync(context);
                
                // 如果上下文已被标记为已处理，停止管线
                if (context.IsHandled)
                {
                    Logger.Debug($"管线在 {handler.GetType().Name} 处理器处提前终止");
                    break;
                }
            }
            catch (Exception ex)
            {
                // 记录错误但继续执行后续处理器（除非是需要阻止的异常）
                Logger.Error(ex, $"管线处理器错误 {handler.GetType().Name}:");
                
                // 如果是需要阻止操作的异常，重新抛出
                throw;
            }
        }
    }
    
    /// <summary>
    /// 执行管线处理器并允许抛出异常来阻止操作
    /// 这个版本适用于验证类处理器（如 RequestStart、JoinRoom 等）
    /// </summary>
    /// <typeparam name="TContext">上下文类型</typeparam>
    /// <param name="handlers">处理器列表</param>
    /// <param name="context">上下文对象</param>
    public static async Task ExecuteWithValidationAsync<TContext>(
        IEnumerable<IPipelineHandler<TContext>> handlers, 
        TContext context) 
        where TContext : IPipelineContext
    {
        // 按优先级排序（数字越小优先级越高）
        var sortedHandlers = handlers
            .OrderBy(h => h.Priority)
            .ToList();
        
        foreach (var handler in sortedHandlers)
        {
            // 对于验证类处理器，任何异常都应该被重新抛出
            await handler.HandleAsync(context);
            
            // 如果上下文已被标记为已处理，停止管线
            if (context.IsHandled)
            {
                Logger.Debug($"验证管线在 {handler.GetType().Name} 处理器处提前终止");
                break;
            }
        }
    }
}

/// <summary>
/// 基础管线上下文 - 提供默认实现
/// </summary>
public abstract class BasePipelineContext : IPipelineContext
{
    /// <inheritdoc/>
    public bool IsHandled { get; set; }
    
    /// <inheritdoc/>
    public object? Result { get; set; }
}
