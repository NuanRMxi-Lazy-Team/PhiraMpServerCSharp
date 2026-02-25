namespace PhiraMp.Server.Plugins;

/// <summary>
/// 适配器：将旧的 IUserLeaveHandler 适配为管线处理器
/// </summary>
internal class UserLeaveHandlerAdapter : IPipelineHandler<UserEventContext>
{
    private readonly IUserLeaveHandler _handler;
    
    public int Priority { get; }
    
    public UserLeaveHandlerAdapter(IUserLeaveHandler handler)
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
        await _handler.HandleUserLeaveAsync(context);
    }
}
