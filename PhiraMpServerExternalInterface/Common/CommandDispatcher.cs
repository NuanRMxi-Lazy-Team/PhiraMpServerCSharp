using System.Collections.Concurrent;

namespace PhiraMpServer.ExternalInterface.Common;

/// <summary>
/// Default implementation of command dispatcher
/// </summary>
public class CommandDispatcher : ICommandDispatcher
{
    private readonly ConcurrentDictionary<Type, object> _handlers = new();

    public void RegisterHandler<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> handler) 
        where TCommand : Command 
        where TResponse : CommandResponse
    {
        var commandType = typeof(TCommand);
        if (!_handlers.TryAdd(commandType, handler))
        {
            throw new InvalidOperationException($"Handler for {commandType.Name} is already registered");
        }
    }

    public async Task<CommandResponse> DispatchAsync(Command command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        var commandType = command.GetType();
        
        if (!_handlers.TryGetValue(commandType, out var handlerObj))
        {
            return new UnknowCommandResponse
            {
                Token = command.Token,
                Message = $"No handler registered for command type: {commandType.Name}"
            };
        }

        try
        {
            // Use reflection to invoke the handler
            var handlerType = handlerObj.GetType();
            var handleMethod = handlerType.GetMethod("HandleAsync");
            
            if (handleMethod == null)
            {
                throw new InvalidOperationException($"HandleAsync method not found on handler for {commandType.Name}");
            }

            var taskObj = handleMethod.Invoke(handlerObj, [command]);
            if (taskObj == null)
            {
                throw new InvalidOperationException($"Handler for {commandType.Name} returned null");
            }

            // Get the Task result dynamically
            var taskType = taskObj.GetType();
            if (!taskType.IsGenericType || taskType.GetGenericTypeDefinition() != typeof(Task<>))
            {
                throw new InvalidOperationException($"Handler for {commandType.Name} did not return a Task<T>");
            }

            // Await the task dynamically
            dynamic dynamicTask = taskObj;
            var response = await dynamicTask as CommandResponse;
            
            // Ensure token is preserved
            if (response != null && string.IsNullOrWhiteSpace(response.Token))
            {
                response.Token = command.Token;
            }

            return response ?? new UnknowCommandResponse
            {
                Token = command.Token,
                Message = $"Handler for {commandType.Name} returned null"
            };
        }
        catch (Exception ex)
        {
            return new UnknowCommandResponse
            {
                Token = command.Token,
                Message = $"Error handling command {commandType.Name}: {ex.Message}"
            };
        }
    }

    public bool HasHandlerForCommand(Type commandType)
    {
        return _handlers.ContainsKey(commandType);
    }
}


