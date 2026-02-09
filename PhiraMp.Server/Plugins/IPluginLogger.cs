namespace PhiraMp.Server.Plugins;

/// <summary>
/// 插件日志接口 - 为插件提供统一的日志记录功能
/// </summary>
public interface IPluginLogger
{
    /// <summary>
    /// 记录调试信息
    /// </summary>
    void Debug(string message);
    
    /// <summary>
    /// 记录普通信息
    /// </summary>
    void Info(string message);
    
    /// <summary>
    /// 记录警告信息
    /// </summary>
    void Warning(string message);
    
    /// <summary>
    /// 记录错误信息
    /// </summary>
    void Error(string message);
    
    /// <summary>
    /// 记录错误信息和异常
    /// </summary>
    void Error(Exception ex, string message);
}

/// <summary>
/// 插件日志实现 - 在日志前添加插件名称标识
/// </summary>
public class PluginLogger : IPluginLogger
{
    private readonly string _pluginName;

    public PluginLogger(string pluginName)
    {
        _pluginName = pluginName;
    }

    public void Debug(string message) => Logger.Debug($"[{_pluginName}] {message}");
    public void Info(string message) => Logger.Info($"[{_pluginName}] {message}");
    public void Warning(string message) => Logger.Warning($"[{_pluginName}] {message}");
    public void Error(string message) => Logger.Error($"[{_pluginName}] {message}");
    public void Error(Exception ex, string message) => Logger.Error(ex, $"[{_pluginName}] {message}");
}
