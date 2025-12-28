namespace PhiraMpServer.ExternalInterface.Common;

/// <summary>
/// Command handler interface. Each command type should have a corresponding handler implementation.
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
public interface ICommandHandler<in TCommand, TResponse> 
    where TCommand : Command 
    where TResponse : CommandResponse
{
    /// <summary>
    /// Handle the command and return a response
    /// </summary>
    Task<TResponse> HandleAsync(TCommand command);
}
