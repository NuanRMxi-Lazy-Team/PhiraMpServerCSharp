using System.Reflection;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 插件加载信息
/// </summary>
public class PluginLoadInfo
{
    /// <summary>插件文件路径</summary>
    public required string Path { get; init; }

    /// <summary>插件程序集</summary>
    public required Assembly Assembly { get; init; }

    /// <summary>插件专属 ALC（isCollectible=true，热重载时可卸载）</summary>
    public PluginLoadContext? LoadContext { get; init; }

    /// <summary>加载时间</summary>
    public DateTime LoadTime { get; init; }
}
