namespace PhiraMp.Server.Plugins;

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
