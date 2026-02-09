using PhiraMp.Plugin.SDK;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// Plugin context implementation
/// </summary>
internal class PluginContext : IPluginContext
{
    public IPluginLogger Logger { get; }
    public IServerAPI ServerAPI { get; }
    public string ConfigDirectory { get; }
    public string DataDirectory { get; }

    public PluginContext(IServerAPI serverAPI, string pluginName, string configRoot, string dataRoot)
    {
        ServerAPI = serverAPI;
        Logger = new PluginLogger(pluginName);
        ConfigDirectory = Path.Combine(configRoot, pluginName);
        DataDirectory = Path.Combine(dataRoot, pluginName);

        Directory.CreateDirectory(ConfigDirectory);
        Directory.CreateDirectory(DataDirectory);
    }
}

/// <summary>
/// Plugin logger implementation
/// </summary>
internal class PluginLogger : IPluginLogger
{
    private readonly string _pluginName;

    public PluginLogger(string pluginName)
    {
        _pluginName = pluginName;
    }

    public void Info(string message)
    {
        Logger.Info($"[{_pluginName}] {message}");
    }

    public void Debug(string message)
    {
        Logger.Debug($"[{_pluginName}] {message}");
    }

    public void Warning(string message)
    {
        Logger.Warning($"[{_pluginName}] {message}");
    }

    public void Error(string message)
    {
        Logger.Error($"[{_pluginName}] {message}");
    }

    public void Error(Exception ex, string message)
    {
        Logger.Error(ex, $"[{_pluginName}] {message}");
    }
}
