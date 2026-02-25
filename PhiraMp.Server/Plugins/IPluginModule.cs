namespace PhiraMp.Server.Plugins;

/// <summary>
/// 插件模块接口（可选）- 插件不一定需要实现此接口
/// 使用 [Export(typeof(IPluginModule))] 让 MEF 发现插件
/// </summary>
public interface IPluginModule
{
    /// <summary>
    /// 插件加载时调用（可选 - 需要时实现）
    /// </summary>
    Task InitializeAsync(PluginContext context);
    
    /// <summary>
    /// 插件卸载时调用（可选 - 需要时实现）
    /// </summary>
    Task ShutdownAsync();
}
