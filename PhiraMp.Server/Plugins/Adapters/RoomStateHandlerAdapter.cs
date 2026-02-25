namespace PhiraMp.Server.Plugins;

/// <summary>
/// 适配器：将旧的 IRoomStateHandler 适配为管线处理器
/// </summary>
internal class RoomStateHandlerAdapter : IPipelineHandler<RoomStateContext>
{
    private readonly IRoomStateHandler _handler;
    
    public int Priority { get; }
    
    public RoomStateHandlerAdapter(IRoomStateHandler handler)
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
    
    public async Task HandleAsync(RoomStateContext context)
    {
        await _handler.HandleStateChangeAsync(context);
    }
}
