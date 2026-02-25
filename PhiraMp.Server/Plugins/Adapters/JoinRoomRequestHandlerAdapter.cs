namespace PhiraMp.Server.Plugins;

/// <summary>
/// 适配器：将旧的 IJoinRoomRequestHandler 适配为管线处理器
/// </summary>
internal class JoinRoomRequestHandlerAdapter : IPipelineHandler<JoinRoomRequestContext>
{
    private readonly IJoinRoomRequestHandler _handler;
    
    public int Priority { get; }
    
    public JoinRoomRequestHandlerAdapter(IJoinRoomRequestHandler handler)
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
    
    public async Task HandleAsync(JoinRoomRequestContext context)
    {
        await _handler.HandleJoinRoomRequestAsync(context);
    }
}
