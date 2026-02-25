namespace PhiraMp.Server.Plugins;

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
