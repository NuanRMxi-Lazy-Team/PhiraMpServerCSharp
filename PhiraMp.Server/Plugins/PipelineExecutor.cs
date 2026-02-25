namespace PhiraMp.Server.Plugins;

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
                Logger.Error(ex, $"管线处理器错误 {handler.GetType().Name}:");
                // 不重新抛出，继续执行后续处理器
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
