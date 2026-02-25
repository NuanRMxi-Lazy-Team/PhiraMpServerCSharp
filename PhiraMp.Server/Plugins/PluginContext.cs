using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 插件上下文 - 在插件初始化时提供
/// </summary>
public class PluginContext
{
    /// <summary>
    /// 服务器状态（直接访问，高级功能）
    /// </summary>
    public ServerState ServerState { get; }
    
    /// <summary>
    /// 插件目录路径
    /// </summary>
    public string PluginDirectory { get; }
    
    /// <summary>
    /// 配置文件目录路径
    /// </summary>
    public string ConfigDirectory { get; }
    
    /// <summary>
    /// 数据文件目录路径
    /// </summary>
    public string DataDirectory { get; }
    
    /// <summary>
    /// 插件 API - 推荐使用此接口操作服务器
    /// </summary>
    public IPluginAPI API { get; }
    
    /// <summary>
    /// 插件日志记录器
    /// </summary>
    public IPluginLogger Logger { get; }
    
    /// <summary>
    /// 插件配置管理器
    /// </summary>
    public IPluginConfig Config { get; }
    
    /// <summary>
    /// 插件服务提供者 - 支持插件间依赖注入
    /// </summary>
    public IPluginServiceProvider ServiceProvider { get; }
    
    public PluginContext(
        ServerState serverState, 
        string pluginDir, 
        string configDir, 
        string dataDir,
        IPluginAPI api,
        IPluginLogger logger,
        IPluginConfig config,
        IPluginServiceProvider serviceProvider)
    {
        ServerState = serverState;
        PluginDirectory = pluginDir;
        ConfigDirectory = configDir;
        DataDirectory = dataDir;
        API = api;
        Logger = logger;
        Config = config;
        ServiceProvider = serviceProvider;
    }
}
