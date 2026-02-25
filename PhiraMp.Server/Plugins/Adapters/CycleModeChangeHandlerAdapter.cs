namespace PhiraMp.Server.Plugins;

/// <summary>
/// 适配器：将旧的 ICycleModeChangeHandler 适配为管线处理器
/// </summary>
internal class CycleModeChangeHandlerAdapter : IPipelineHandler<CycleModeChangeContext>
{
    private readonly ICycleModeChangeHandler _handler;
    
    public int Priority { get; }
    
    public CycleModeChangeHandlerAdapter(ICycleModeChangeHandler handler)
    {
        _handler = handler;
        
        if (handler is IPrioritizedHandler prioritized)
        {
            Priority = prioritized.Priority;
        }
        else
        {
            Priority = 100;
        }
    }
    
    public async Task HandleAsync(CycleModeChangeContext context)
    {
        await _handler.HandleCycleModeChangeAsync(context);
    }
}
