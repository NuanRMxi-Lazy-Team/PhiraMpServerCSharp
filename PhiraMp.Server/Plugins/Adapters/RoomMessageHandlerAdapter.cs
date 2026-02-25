namespace PhiraMp.Server.Plugins;

/// <summary>
/// 适配器：将旧的 IRoomMessageHandler 适配为管线处理器
/// </summary>
internal class RoomMessageHandlerAdapter : IPipelineHandler<RoomMessageContext>
{
    private readonly IRoomMessageHandler _handler;
    
    public int Priority { get; }
    
    public RoomMessageHandlerAdapter(IRoomMessageHandler handler)
    {
        _handler = handler;
        
        // 如果处理器实现了优先级接口，使用它的优先级
        if (handler is IPrioritizedHandler prioritized)
        {
            Priority = prioritized.Priority;
        }
        else
        {
            Priority = 100; // 默认优先级
        }
    }
    
    public async Task HandleAsync(RoomMessageContext context)
    {
        await _handler.HandleMessageAsync(context);
    }
}
