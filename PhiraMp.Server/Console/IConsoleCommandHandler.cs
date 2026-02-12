namespace PhiraMp.Server.Console;

/// <summary>
/// 控制台命令处理器接口 - 插件可导出此接口以注册控制台命令
/// 使用 [Export(typeof(IConsoleCommandHandler))] 注册
/// </summary>
public interface IConsoleCommandHandler
{
    /// <summary>
    /// 注册插件的控制台命令
    /// </summary>
    /// <param name="commandSystem">控制台命令系统</param>
    void RegisterCommands(ConsoleCommandSystem commandSystem);
}

