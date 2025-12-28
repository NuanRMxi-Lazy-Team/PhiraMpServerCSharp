using System;
using System.Collections.Generic;
using System.Linq;
using PhiraMpServer.ExternalInterface.Common;

namespace PhiraMpServer.Server;

/// <summary>
/// 命令分发器注册验证的扩展方法
/// </summary>
public static class CommandDispatcherExtensions
{
    /// <param name="dispatcher">命令分发器</param>
    extension(ICommandDispatcher dispatcher)
    {
        /// <summary>
        /// 验证所有命令类型是否都有已注册的处理器
        /// </summary>
        /// <returns>没有处理器的命令类型列表</returns>
        public List<Type> ValidateAllCommandsHaveHandlers()
        {
            // 从 Commands 程序集中获取所有命令类型
            var commandTypes = typeof(Command).Assembly
                .GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(Command)))
                .ToList();

            var missingHandlers = new List<Type>();
        
            // 检查每个命令类型
            foreach (var commandType in commandTypes)
            {
                // 尝试分发测试命令以查看处理器是否存在
                var testCommand = Activator.CreateInstance(commandType) as Command;
                if (testCommand != null)
                {
                    testCommand.Token = "__VALIDATION__";
                    var result = dispatcher.DispatchAsync(testCommand).Result;
                
                    // 如果结果是 UnknowCommandResponse，则表示处理器缺失
                    if (result is UnknowCommandResponse unknownResponse && 
                        unknownResponse.Message.Contains("No handler registered"))
                    {
                        missingHandlers.Add(commandType);
                    }
                }
            }

            return missingHandlers;
        }

        /// <summary>
        /// 确保所有命令都有已注册的处理器，如果有缺失则抛出异常
        /// </summary>
        public void EnsureAllCommandsHaveHandlers()
        {
            var missingHandlers = dispatcher.ValidateAllCommandsHaveHandlers();

            if (missingHandlers.Count <= 0) return;
            var commandNames = string.Join(", ", missingHandlers.Select(t => t.Name));
            throw new InvalidOperationException(
                $"以下命令没有注册处理器: {commandNames}. " +
                "请为每个命令实现 ICommandHandler<TCommand, TResponse> 并在分发器中注册。");
        }
    }
}

/// <summary>
/// 用于配置所有命令处理器的辅助类
/// </summary>
public static class CommandHandlerConfiguration
{
    /// <summary>
    /// 为服务器注册所有命令处理器
    /// </summary>
    public static void RegisterAllHandlers(ICommandDispatcher dispatcher, ServerState serverState, DateTime startTime)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(serverState);

        // 注册所有处理器
        dispatcher.RegisterHandler(new AuthenticateHandler(serverState));
        dispatcher.RegisterHandler(new GetAllRoomHandler(serverState));
        dispatcher.RegisterHandler(new SetServerRoomMaxPlayersHandler(serverState));
        dispatcher.RegisterHandler(new GetServerStatusHandler(serverState, startTime));
        dispatcher.RegisterHandler(new GetAllPlayersHandler(serverState));
        
        // 在此处添加更多处理器
        // dispatcher.RegisterHandler(new YourNewCommandHandler(serverState));
        
        // 验证所有命令都有处理器
        dispatcher.EnsureAllCommandsHaveHandlers();
    }
}

