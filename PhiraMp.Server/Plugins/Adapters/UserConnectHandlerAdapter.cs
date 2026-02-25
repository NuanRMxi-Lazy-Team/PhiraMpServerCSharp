namespace PhiraMp.Server.Plugins;

/// <summary>
/// 适配器：将 IUserConnectHandler 适配为管线处理器
/// </summary>
internal class UserConnectHandlerAdapter : IPipelineHandler<UserConnectContext>
{
    private readonly IUserConnectHandler _handler;

    public int Priority { get; }

    public UserConnectHandlerAdapter(IUserConnectHandler handler)
    {
        _handler = handler;
        Priority = handler is IPrioritizedHandler p ? p.Priority : 100;
    }

    public async Task HandleAsync(UserConnectContext context)
    {
        await _handler.HandleUserConnectAsync(context);
    }
}
