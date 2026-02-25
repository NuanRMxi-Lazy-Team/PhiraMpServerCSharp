namespace PhiraMp.Server.Plugins;

/// <summary>
/// 适配器：将旧的 ISelectChartHandler 适配为管线处理器
/// </summary>
internal class SelectChartHandlerAdapter : IPipelineHandler<SelectChartContext>
{
    private readonly ISelectChartHandler _handler;
    
    public int Priority { get; }
    
    public SelectChartHandlerAdapter(ISelectChartHandler handler)
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
    
    public async Task HandleAsync(SelectChartContext context)
    {
        await _handler.HandleSelectChartAsync(context);
    }
}
