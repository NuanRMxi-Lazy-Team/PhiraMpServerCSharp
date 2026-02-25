using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 插件配置接口 - 为插件提供配置文件读写功能
/// </summary>
public interface IPluginConfig
{
    /// <summary>
    /// 加载配置文件，如果不存在则创建默认配置
    /// </summary>
    T Load<T>(string fileName, T defaultValue) where T : class, new();
    
    /// <summary>
    /// 异步加载配置文件，如果不存在则创建默认配置
    /// </summary>
    Task<T> LoadAsync<T>(string fileName, T defaultValue) where T : class, new();
    
    /// <summary>
    /// 保存配置到文件
    /// </summary>
    void Save<T>(string fileName, T config) where T : class;
    
    /// <summary>
    /// 检查配置文件是否存在
    /// </summary>
    bool Exists(string fileName);
    
    /// <summary>
    /// 删除配置文件
    /// </summary>
    void Delete(string fileName);
}

