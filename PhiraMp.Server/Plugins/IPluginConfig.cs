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

/// <summary>
/// 插件配置实现 - 使用 YAML 格式存储配置
/// </summary>
public class PluginConfig : IPluginConfig
{
    private readonly string _configDirectory;
    private readonly IPluginLogger _logger;

    public PluginConfig(string configDirectory, IPluginLogger logger)
    {
        _configDirectory = configDirectory;
        _logger = logger;
        Directory.CreateDirectory(_configDirectory);
    }

    public T Load<T>(string fileName, T defaultValue) where T : class, new()
    {
        var filePath = Path.Combine(_configDirectory, fileName);
        
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.Info($"配置文件不存在，创建默认配置: {fileName}");
                Save(fileName, defaultValue);
                return defaultValue;
            }

            var yaml = File.ReadAllText(filePath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            
            var config = deserializer.Deserialize<T>(yaml);
            _logger.Debug($"已加载配置文件: {fileName}");
            return config ?? defaultValue;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"加载配置文件失败: {fileName}");
            return defaultValue;
        }
    }
    
    public async Task<T> LoadAsync<T>(string fileName, T defaultValue) where T : class, new()
    {
        var filePath = Path.Combine(_configDirectory, fileName);
        
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.Info($"配置文件不存在，创建默认配置: {fileName}");
                Save(fileName, defaultValue);
                return defaultValue;
            }

            var yaml = await File.ReadAllTextAsync(filePath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            
            var config = deserializer.Deserialize<T>(yaml);
            _logger.Debug($"已加载配置文件: {fileName}");
            return config ?? defaultValue;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"加载配置文件失败: {fileName}");
            return defaultValue;
        }
    }

    public void Save<T>(string fileName, T config) where T : class
    {
        var filePath = Path.Combine(_configDirectory, fileName);
        
        try
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            
            var yaml = serializer.Serialize(config);
            File.WriteAllText(filePath, yaml);
            _logger.Debug($"已保存配置文件: {fileName}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"保存配置文件失败: {fileName}");
        }
    }

    public bool Exists(string fileName)
    {
        var filePath = Path.Combine(_configDirectory, fileName);
        return File.Exists(filePath);
    }

    public void Delete(string fileName)
    {
        var filePath = Path.Combine(_configDirectory, fileName);
        
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.Debug($"已删除配置文件: {fileName}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"删除配置文件失败: {fileName}");
        }
    }
}
