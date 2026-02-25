using System.ComponentModel.Composition;
using PhiraMp.Server.Plugins;

namespace PhiraMp.Plugins.PipelineDemo;

/// <summary>
/// 低优先级消息处理器 - 用于对比演示
/// 此处理器的优先级较低，将在 PipelineDemoPlugin 之后执行
/// 如果前面的处理器设置了 IsHandled=true，此处理器将不会被调用
/// </summary>
[Export(typeof(IRoomMessageHandler))]
public class LowPriorityMessageHandler : IRoomMessageHandler, IPrioritizedHandler
{
    /// <summary>
    /// 设置较低优先级
    /// </summary>
    public int Priority => 200;

    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        var message = context.Message.Trim();
        
        if (message.StartsWith("/pipeline", StringComparison.OrdinalIgnoreCase))
        {
            // 如果能执行到这里，说明高优先级处理器没有拦截
            // 使用全局 Logger 而不是实例字段
            PhiraMp.Server.Logger.Debug($"[低优先级处理器] 收到消息: {message}");
            PhiraMp.Server.Logger.Debug($"[低优先级处理器] IsHandled={context.IsHandled}");
        }
        
        await Task.CompletedTask;
    }
}
