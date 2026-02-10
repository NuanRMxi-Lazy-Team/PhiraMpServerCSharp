using System.Collections.Concurrent;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 插件服务提供者接口 - 支持插件间的依赖注入
/// </summary>
public interface IPluginServiceProvider
{
    /// <summary>
    /// 注册服务（由插件调用以提供服务）
    /// </summary>
    /// <typeparam name="TService">服务接口类型</typeparam>
    /// <param name="implementation">服务实现实例</param>
    void RegisterService<TService>(TService implementation) where TService : class;
    
    /// <summary>
    /// 获取服务（由插件调用以获取其他插件提供的服务）
    /// </summary>
    /// <typeparam name="TService">服务接口类型</typeparam>
    /// <returns>服务实例，如果未注册则返回 null</returns>
    TService? GetService<TService>() where TService : class;
    
    /// <summary>
    /// 获取所有已注册的服务类型
    /// </summary>
    IEnumerable<Type> GetRegisteredServiceTypes();
    
    /// <summary>
    /// 检查服务是否已注册
    /// </summary>
    bool IsServiceRegistered<TService>() where TService : class;
}

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
