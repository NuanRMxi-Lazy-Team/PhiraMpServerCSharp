namespace PhiraMp.Server.Plugins;

/// <summary>
/// 适配器：将 IUserDisconnectHandler 适配为管线处理器
/// </summary>
internal class UserDisconnectHandlerAdapter : IPipelineHandler<UserDisconnectContext>
{
    private readonly IUserDisconnectHandler _handler;

    public int Priority { get; }

    public UserDisconnectHandlerAdapter(IUserDisconnectHandler handler)
    {
        _handler = handler;
        Priority = handler is IPrioritizedHandler p ? p.Priority : 100;
    }

    public async Task HandleAsync(UserDisconnectContext context)
    {
        await _handler.HandleUserDisconnectAsync(context);
    }
}
