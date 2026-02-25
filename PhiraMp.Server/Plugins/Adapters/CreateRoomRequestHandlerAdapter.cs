namespace PhiraMp.Server.Plugins;

/// <summary>
/// 适配器：将旧的 ICreateRoomRequestHandler 适配为管线处理器
/// </summary>
internal class CreateRoomRequestHandlerAdapter : IPipelineHandler<CreateRoomRequestContext>
{
    private readonly ICreateRoomRequestHandler _handler;
    
    public int Priority { get; }
    
    public CreateRoomRequestHandlerAdapter(ICreateRoomRequestHandler handler)
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
    
    public async Task HandleAsync(CreateRoomRequestContext context)
    {
        await _handler.HandleCreateRoomRequestAsync(context);
    }
}
