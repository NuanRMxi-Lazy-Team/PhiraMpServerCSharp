using System.Collections.Concurrent;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 插件服务提供者实现 - 管理插件间的服务依赖
/// </summary>
public class PluginServiceProvider : IPluginServiceProvider
{
    // 使用 ConcurrentDictionary 保证线程安全
    private readonly ConcurrentDictionary<Type, object> _services = new();
    
    /// <inheritdoc/>
    public void RegisterService<TService>(TService implementation) where TService : class
    {
        if (implementation == null)
            throw new ArgumentNullException(nameof(implementation));
        
        var serviceType = typeof(TService);
        
        if (_services.TryAdd(serviceType, implementation))
        {
            Logger.Info($"插件服务已注册: {serviceType.Name}");
        }
        else
        {
            // 如果服务已经存在，更新它
            _services[serviceType] = implementation;
            Logger.Warning($"插件服务已更新: {serviceType.Name}");
        }
    }
    
    /// <inheritdoc/>
    public TService? GetService<TService>() where TService : class
    {
        var serviceType = typeof(TService);
        
        if (_services.TryGetValue(serviceType, out var service))
        {
            return service as TService;
        }
        
        Logger.Debug($"未找到插件服务: {serviceType.Name}");
        return null;
    }
    
    /// <inheritdoc/>
    public IEnumerable<Type> GetRegisteredServiceTypes()
    {
        return _services.Keys;
    }
    
    /// <inheritdoc/>
    public bool IsServiceRegistered<TService>() where TService : class
    {
        return _services.ContainsKey(typeof(TService));
    }
    
    /// <summary>
    /// 清空所有服务（用于插件重载）
    /// </summary>
    public void ClearServices()
    {
        var count = _services.Count;
        _services.Clear();
        Logger.Info($"已清空 {count} 个插件服务");
    }
    
    /// <summary>
    /// 获取所有已注册的服务信息（用于调试）
    /// </summary>
    public string GetServicesSummary()
    {
        if (_services.IsEmpty)
            return "无已注册服务";
        
        var services = _services.Keys.Select(t => t.Name);
        return $"已注册服务 ({_services.Count}): {string.Join(", ", services)}";
    }
}
