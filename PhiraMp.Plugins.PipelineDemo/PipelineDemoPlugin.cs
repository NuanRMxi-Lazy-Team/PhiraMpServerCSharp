using System.ComponentModel.Composition;
using PhiraMp.Server.Models;
using PhiraMp.Server.Plugins;

namespace PhiraMp.Plugins.PipelineDemo;

/// <summary>
/// 管线演示插件 - 展示如何使用管线系统实现提前返回
/// 此插件演示两个特性：
/// 1. 使用优先级控制处理器执行顺序
/// 2. 使用 IsHandled 标记提前终止管线
/// </summary>
[Export(typeof(IPluginModule))]
[Export(typeof(IRoomMessageHandler))]
public class PipelineDemoPlugin : IPluginModule, IRoomMessageHandler, IPrioritizedHandler
{
    private IPluginLogger _logger = null!;
    private IPluginAPI _api = null!;

    /// <summary>
    /// 设置较高优先级（数字越小优先级越高）
    /// 这个处理器将在其他普通处理器之前执行
    /// </summary>
    public int Priority => 10;

    /// <summary>
    /// 插件初始化
    /// </summary>
    public async Task InitializeAsync(PluginContext context)
    {
        _logger = context.Logger;
        _api = context.API;

        _logger.Info("管线演示插件已初始化");
        _logger.Info($"- 优先级: {Priority}（数字越小优先级越高）");
        _logger.Info("- 命令: /pipeline_test - 测试管线提前返回");
        _logger.Info("- 命令: /pipeline_normal - 正常消息，继续执行管线");
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理房间消息
    /// </summary>
    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        var message = context.Message.Trim();
        
        // 示例 1: 完全拦截消息，不让其他插件处理
        if (message.StartsWith("/pipeline_test", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Info($"用户 {context.User.Name} 触发管线测试命令");
            
            // 发送响应消息
            await _api.SendRoomMessageAsync(
                context.Room, 
                $"[管线演示] 此消息由高优先级处理器拦截，其他处理器将不会收到此消息。");
            
            // 标记为已处理，停止管线继续执行
            context.IsHandled = true;
            
            _logger.Info("已标记 IsHandled=true，管线将在此处终止");
            return;
        }
        
        // 示例 2: 记录消息但不拦截，让管线继续
        if (message.StartsWith("/pipeline_normal", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Info($"用户 {context.User.Name} 发送正常消息，将继续执行管线");
            
            await _api.SendRoomMessageAsync(
                context.Room, 
                $"[管线演示] 此消息将继续传递给其他处理器。");
            
            // 不设置 IsHandled，管线继续执行
            return;
        }
        
        // 其他消息：记录但不处理
        if (message.StartsWith("/"))
        {
            _logger.Debug($"高优先级处理器观察到命令: {message}");
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// 插件关闭
    /// </summary>
    public async Task ShutdownAsync()
    {
        _logger.Info("管线演示插件已关闭");
        await Task.CompletedTask;
    }
}

