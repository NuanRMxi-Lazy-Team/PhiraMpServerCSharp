# 插件系统进化：管线 + 依赖注入

## 概述

PhiraMp 服务器插件系统已升级，现在支持：

1. **管线系统（Pipeline System）** - 插件可以控制执行流程，实现提前返回
2. **依赖注入（Dependency Injection）** - 插件之间可以互相依赖，共享服务

这些新特性使得插件更加强大和灵活，同时保持了向后兼容性。

## 一、管线系统

### 1.1 什么是管线系统？

管线系统允许多个插件按照**优先级顺序**处理同一个事件。插件可以：
- 设置自己的优先级（数字越小优先级越高）
- 标记事件为"已处理"，阻止后续插件继续处理
- 完全控制事件流程

### 1.2 核心接口

#### IPipelineContext
所有上下文类都继承此接口，提供管线控制：

```csharp
public interface IPipelineContext
{
    /// <summary>
    /// 标记此请求是否已被处理（已处理则停止管线）
    /// </summary>
    bool IsHandled { get; set; }
    
    /// <summary>
    /// 处理结果（可选）
    /// </summary>
    object? Result { get; set; }
}
```

#### IPrioritizedHandler
插件可以实现此接口来设置优先级：

```csharp
public interface IPrioritizedHandler
{
    /// <summary>
    /// 处理器优先级（数字越小优先级越高）
    /// 默认：100
    /// </summary>
    int Priority { get; }
}
```

### 1.3 使用示例

#### 示例 1：高优先级拦截器

```csharp
[Export(typeof(IRoomMessageHandler))]
public class SecurityPlugin : IRoomMessageHandler, IPrioritizedHandler
{
    // 设置最高优先级
    public int Priority => 1;
    
    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        // 检查是否包含敏感词
        if (ContainsBadWord(context.Message))
        {
            await SendWarning(context.User);
            
            // 标记为已处理，阻止其他插件看到此消息
            context.IsHandled = true;
            return;
        }
    }
}
```

#### 示例 2：正常优先级处理器

```csharp
[Export(typeof(IRoomMessageHandler))]
public class CommandPlugin : IRoomMessageHandler, IPrioritizedHandler
{
    // 默认优先级
    public int Priority => 100;
    
    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        // 如果前面的插件已经处理，这里不会被调用
        if (context.Message.StartsWith("/help"))
        {
            await ShowHelp(context.Room);
            // 可以选择标记为已处理
            context.IsHandled = true;
        }
    }
}
```

### 1.4 优先级建议

| 优先级范围 | 用途 | 示例 |
|----------|------|------|
| 1-10 | 安全/验证 | 敏感词过滤、权限检查 |
| 11-50 | 核心功能 | 命令路由、特殊逻辑 |
| 51-100 | 普通插件 | 大部分插件（默认） |
| 101-200 | 低优先级 | 日志记录、统计 |

## 二、依赖注入系统

### 2.1 什么是依赖注入？

依赖注入允许插件之间共享服务，实现插件间通信和协作。

**核心概念：**
- **服务提供者（Provider）**：注册并提供服务的插件
- **服务消费者（Consumer）**：使用其他插件提供的服务的插件
- **服务接口（Interface）**：定义服务契约

### 2.2 核心接口

```csharp
public interface IPluginServiceProvider
{
    /// <summary>
    /// 注册服务（由提供者插件调用）
    /// </summary>
    void RegisterService<TService>(TService implementation) where TService : class;
    
    /// <summary>
    /// 获取服务（由消费者插件调用）
    /// </summary>
    TService? GetService<TService>() where TService : class;
    
    /// <summary>
    /// 检查服务是否已注册
    /// </summary>
    bool IsServiceRegistered<TService>() where TService : class;
}
```

### 2.3 使用步骤

#### 步骤 1：定义服务接口

```csharp
// 在提供者插件中定义接口
public interface IPlayerStatsService
{
    void RecordEvent(string username, string eventType);
    int GetEventCount(string username);
}
```

#### 步骤 2：实现并注册服务

```csharp
[Export(typeof(IPluginModule))]
public class StatsProviderPlugin : IPluginModule
{
    private IPluginServiceProvider _serviceProvider = null!;
    private readonly PlayerStatsService _service = new();
    
    public async Task InitializeAsync(PluginContext context)
    {
        _serviceProvider = context.ServiceProvider;
        
        // 注册服务
        _serviceProvider.RegisterService<IPlayerStatsService>(_service);
        
        context.Logger.Info("统计服务已注册");
    }
}

// 服务实现
internal class PlayerStatsService : IPlayerStatsService
{
    public void RecordEvent(string username, string eventType)
    {
        // 实现逻辑
    }
    
    public int GetEventCount(string username)
    {
        // 实现逻辑
        return 0;
    }
}
```

#### 步骤 3：使用服务

```csharp
[Export(typeof(IPluginModule))]
[Export(typeof(IRoomMessageHandler))]
public class StatsConsumerPlugin : IPluginModule, IRoomMessageHandler
{
    private IPlayerStatsService? _statsService;
    
    public async Task InitializeAsync(PluginContext context)
    {
        // 获取服务
        _statsService = context.ServiceProvider.GetService<IPlayerStatsService>();
        
        if (_statsService != null)
        {
            context.Logger.Info("成功获取统计服务");
        }
        else
        {
            context.Logger.Warning("统计服务不可用");
        }
    }
    
    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        if (_statsService != null)
        {
            _statsService.RecordEvent(context.User.Name, "message");
        }
    }
}
```

### 2.4 最佳实践

1. **定义清晰的接口**
   - 接口应该简单明了
   - 避免过度设计

2. **检查服务可用性**
   - 始终检查 `GetService()` 返回值
   - 处理服务不可用的情况

3. **服务生命周期**
   - 所有服务都是单例
   - 插件重载时服务会被清空

4. **线程安全**
   - 服务实现需要保证线程安全
   - 使用锁或并发集合

## 三、示例插件

### 3.1 管线演示插件

位置：`PhiraMp.Plugins.PipelineDemo`

功能：
- 演示高优先级和低优先级处理器
- 演示如何使用 `IsHandled` 提前终止管线
- 命令：
  - `/pipeline_test` - 触发高优先级拦截
  - `/pipeline_normal` - 正常通过管线

### 3.2 依赖注入演示插件

位置：`PhiraMp.Plugins.DependencyInjectionDemo`

功能：
- 演示服务提供者和消费者
- 提供玩家统计服务
- 命令：
  - `/stats [玩家名]` - 查看玩家统计
  - `/stats_total` - 查看总体统计

## 四、向后兼容性

所有现有插件**无需修改**即可继续工作：

1. **不实现 IPrioritizedHandler** - 使用默认优先级 100
2. **不设置 IsHandled** - 管线正常执行完所有处理器
3. **不使用 ServiceProvider** - 插件独立工作

## 五、高级用法

### 5.1 动态优先级

```csharp
public class DynamicPriorityHandler : IRoomMessageHandler, IPrioritizedHandler
{
    private int _priority = 100;
    
    public int Priority => _priority;
    
    public void SetPriority(int priority)
    {
        _priority = priority;
        // 注意：需要重新加载插件才能生效
    }
}
```

### 5.2 条件性提前返回

```csharp
public async Task HandleMessageAsync(RoomMessageContext context)
{
    if (ShouldIntercept(context))
    {
        await HandleInterception(context);
        context.IsHandled = true; // 阻止后续处理
        return;
    }
    
    // 正常处理，不阻止其他插件
    await NormalProcessing(context);
}
```

### 5.3 服务链

```csharp
// 服务 A
public interface IServiceA
{
    void DoSomething();
}

// 服务 B 依赖服务 A
public class ServiceB : IServiceB
{
    private readonly IServiceA _serviceA;
    
    public ServiceB(IServiceA serviceA)
    {
        _serviceA = serviceA;
    }
    
    public void DoOtherThing()
    {
        _serviceA.DoSomething();
    }
}
```

## 六、调试技巧

### 6.1 查看管线执行顺序

在处理器中添加日志：

```csharp
public async Task HandleMessageAsync(RoomMessageContext context)
{
    Logger.Debug($"[{GetType().Name}] Priority={Priority}, IsHandled={context.IsHandled}");
    // 处理逻辑
}
```

### 6.2 查看已注册服务

```csharp
var serviceTypes = context.ServiceProvider.GetRegisteredServiceTypes();
foreach (var type in serviceTypes)
{
    context.Logger.Info($"已注册服务: {type.Name}");
}
```

## 七、常见问题

**Q: 如何确保我的插件最先执行？**
A: 设置 `Priority = 1`，数字越小优先级越高。

**Q: 如果两个插件优先级相同怎么办？**
A: 执行顺序不确定，建议使用不同的优先级。

**Q: 服务什么时候被清理？**
A: 插件重新加载时，所有服务都会被清空。

**Q: 可以动态注册服务吗？**
A: 可以，但建议在 `InitializeAsync` 中注册。

**Q: IsHandled 会影响所有类型的处理器吗？**
A: 是的，管线会立即停止，不再调用后续处理器。

## 八、迁移指南

如果你想让现有插件使用新特性：

### 添加优先级

```csharp
// 之前
[Export(typeof(IRoomMessageHandler))]
public class MyPlugin : IRoomMessageHandler
{
    // ...
}

// 之后
[Export(typeof(IRoomMessageHandler))]
public class MyPlugin : IRoomMessageHandler, IPrioritizedHandler
{
    public int Priority => 50; // 添加优先级
    // ...
}
```

### 提供服务

```csharp
public async Task InitializeAsync(PluginContext context)
{
    // 注册服务供其他插件使用
    context.ServiceProvider.RegisterService<IMyService>(_myService);
}
```

### 使用服务

```csharp
public async Task InitializeAsync(PluginContext context)
{
    // 获取其他插件提供的服务
    var service = context.ServiceProvider.GetService<IMyService>();
    if (service != null)
    {
        // 使用服务
    }
}
```

## 九、总结

新的管线和依赖注入系统为插件开发提供了更强大的能力：

- ✅ **管线系统** - 控制执行流程，实现复杂的处理逻辑
- ✅ **依赖注入** - 插件间协作，构建更大的功能
- ✅ **向后兼容** - 现有插件无需修改
- ✅ **易于使用** - 简单的 API，清晰的概念

开始使用这些新特性，构建更强大的插件吧！
