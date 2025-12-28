using System;
using System.Threading.Tasks;

namespace PhiraMpServer.ExternalInterface.Common;

public interface ICommandDispatcher
{
    /// <summary>
    /// 注册特定命令类型的处理器
    /// </summary>
    void RegisterHandler<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> handler) 
        where TCommand : Command 
        where TResponse : CommandResponse;
    
    /// <summary>
    /// 分发命令到其注册的处理器
    /// </summary>
    Task<CommandResponse> DispatchAsync(Command command);
    
    /// <summary>
    /// 检查指定命令类型是否有已注册的处理器
    /// </summary>
    bool HasHandlerForCommand(Type commandType);
}