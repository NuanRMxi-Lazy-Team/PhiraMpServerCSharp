namespace PhiraMpServer.ExternalInterface.Common;

/// <summary>
/// Command dispatcher interface for routing commands to their handlers
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Register a handler for a specific command type
    /// </summary>
    void RegisterHandler<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> handler) 
        where TCommand : Command 
        where TResponse : CommandResponse;
    
    /// <summary>
    /// Dispatch a command to its registered handler
    /// </summary>
    Task<CommandResponse> DispatchAsync(Command command);
}

