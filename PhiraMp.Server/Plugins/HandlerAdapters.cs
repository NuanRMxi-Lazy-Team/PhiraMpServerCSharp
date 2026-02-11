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

/// <summary>
/// 适配器：将旧的 ISelectChartHandler 适配为管线处理器
/// </summary>
internal class SelectChartHandlerAdapter : IPipelineHandler<SelectChartContext>
{
    private readonly ISelectChartHandler _handler;
    
    public int Priority { get; }
    
    public SelectChartHandlerAdapter(ISelectChartHandler handler)
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
    
    public async Task HandleAsync(SelectChartContext context)
    {
        await _handler.HandleSelectChartAsync(context);
    }
}

/// <summary>
/// 适配器：将旧的 ICycleModeChangeHandler 适配为管线处理器
/// </summary>
internal class CycleModeChangeHandlerAdapter : IPipelineHandler<CycleModeChangeContext>
{
    private readonly ICycleModeChangeHandler _handler;
    
    public int Priority { get; }
    
    public CycleModeChangeHandlerAdapter(ICycleModeChangeHandler handler)
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
    
    public async Task HandleAsync(CycleModeChangeContext context)
    {
        await _handler.HandleCycleModeChangeAsync(context);
    }
}

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

/// <summary>
/// 优先级接口 - 插件处理器可以实现此接口来指定优先级
/// </summary>
public interface IPrioritizedHandler
{
    /// <summary>
    /// 处理器优先级（数字越小优先级越高）
    /// </summary>
    int Priority { get; }
}
