namespace PhiraMp.Server.Console;

/// <summary>
/// 控制台命令定义
/// </summary>
public class ConsoleCommand
{
    /// <summary>命令名称</summary>
    public string Name { get; }
    
    /// <summary>命令描述</summary>
    public string Description { get; }
    
    /// <summary>命令用法示例</summary>
    public string Usage { get; }
    
    /// <summary>命令执行器</summary>
    private readonly Func<string[], Task> _executor;
    
    public ConsoleCommand(string name, string description, string usage, Func<string[], Task> executor)
    {
        Name = name;
        Description = description;
        Usage = usage;
        _executor = executor;
    }
    
    /// <summary>
    /// 执行命令
    /// </summary>
    /// <param name="args">命令参数</param>
    public async Task ExecuteAsync(string[] args)
    {
        await _executor(args);
    }
}
