namespace PhiraMp.Server.Plugins;

/// <summary>
/// 适配器：将旧的 IAuthenticationHandler 适配为管线处理器
/// </summary>
internal class AuthenticationHandlerAdapter : IPipelineHandler<AuthenticationContext>
{
    private readonly IAuthenticationHandler _handler;
    
    public int Priority { get; }
    
    public AuthenticationHandlerAdapter(IAuthenticationHandler handler)
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
    
    public async Task HandleAsync(AuthenticationContext context)
    {
        await _handler.HandleAuthenticationAsync(context);
    }
}
