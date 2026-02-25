namespace PhiraMp.Server.Plugins;

/// <summary>
/// 适配器：将旧的 IUserJoinHandler 适配为管线处理器
/// </summary>
internal class UserJoinHandlerAdapter : IPipelineHandler<UserEventContext>
{
    private readonly IUserJoinHandler _handler;
    
    public int Priority { get; }
    
    public UserJoinHandlerAdapter(IUserJoinHandler handler)
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
    
    public async Task HandleAsync(UserEventContext context)
    {
        await _handler.HandleUserJoinAsync(context);
    }
}
