namespace PhiraMp.Server.Plugins;

/// <summary>
/// 适配器：将旧的 IRequestStartHandler 适配为管线处理器
/// </summary>
internal class RequestStartHandlerAdapter : IPipelineHandler<RequestStartContext>
{
    private readonly IRequestStartHandler _handler;
    
    public int Priority { get; }
    
    public RequestStartHandlerAdapter(IRequestStartHandler handler)
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
    
    public async Task HandleAsync(RequestStartContext context)
    {
        await _handler.HandleRequestStartAsync(context);
    }
}
