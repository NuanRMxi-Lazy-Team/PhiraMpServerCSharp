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
