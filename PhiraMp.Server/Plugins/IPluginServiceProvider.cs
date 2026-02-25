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
