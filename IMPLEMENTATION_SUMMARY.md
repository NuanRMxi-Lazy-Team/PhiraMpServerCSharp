# 插件系统升级总结

## 更新内容

本次更新实现了用户要求的**管线系统 + 依赖注入**，使插件系统更加强大和灵活。

## 实现的功能

### 1. 管线系统（Pipeline System）

插件现在可以：
- ⭐ **控制执行顺序**：通过 `IPrioritizedHandler` 接口设置优先级
- ⭐ **提前终止管线**：设置 `context.IsHandled = true` 阻止后续插件执行
- ⭐ **传递结果**：通过 `context.Result` 在插件间传递数据

**关键接口：**
```csharp
// 管线上下文（所有上下文类都继承此接口）
public interface IPipelineContext
{
    bool IsHandled { get; set; }    // 标记是否已处理
    object? Result { get; set; }    // 处理结果
}

// 优先级接口（可选实现）
public interface IPrioritizedHandler
{
    int Priority { get; }  // 数字越小优先级越高，默认 100
}
```

**使用示例：**
```csharp
[Export(typeof(IRoomMessageHandler))]
public class SecurityPlugin : IRoomMessageHandler, IPrioritizedHandler
{
    public int Priority => 1;  // 最高优先级，最先执行
    
    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        if (ContainsBadWord(context.Message))
        {
            await WarnUser(context.User);
            context.IsHandled = true;  // 阻止其他插件处理此消息
        }
    }
}
```

### 2. 依赖注入系统（Dependency Injection）

插件现在可以：
- ⭐ **注册服务**：提供功能供其他插件使用
- ⭐ **获取服务**：使用其他插件提供的功能
- ⭐ **插件协作**：构建更复杂的功能组合

**关键接口：**
```csharp
public interface IPluginServiceProvider
{
    void RegisterService<T>(T implementation);  // 注册服务
    T? GetService<T>();                        // 获取服务
    bool IsServiceRegistered<T>();             // 检查服务
}
```

**使用示例：**

定义服务接口：
```csharp
public interface IPlayerStatsService
{
    void RecordEvent(string username, string eventType);
    int GetEventCount(string username);
}
```

提供服务：
```csharp
[Export(typeof(IPluginModule))]
public class StatsProviderPlugin : IPluginModule
{
    public async Task InitializeAsync(PluginContext context)
    {
        var service = new PlayerStatsService();
        context.ServiceProvider.RegisterService<IPlayerStatsService>(service);
    }
}
```

使用服务：
```csharp
[Export(typeof(IPluginModule))]
public class StatsConsumerPlugin : IPluginModule
{
    private IPlayerStatsService? _statsService;
    
    public async Task InitializeAsync(PluginContext context)
    {
        _statsService = context.ServiceProvider.GetService<IPlayerStatsService>();
    }
}
```

## 向后兼容性

✅ **所有现有插件无需修改**即可继续工作！

- 不实现 `IPrioritizedHandler` → 使用默认优先级 100
- 不使用 `IsHandled` → 管线正常执行所有处理器
- 不使用 `ServiceProvider` → 插件独立工作

## 优先级建议

| 范围 | 用途 | 示例 |
|-----|------|------|
| 1-10 | 安全/验证 | 敏感词过滤、权限检查 |
| 11-50 | 核心功能 | 命令路由、特殊逻辑 |
| 51-100 | 普通插件 | 大部分插件（默认） |
| 101-200 | 低优先级 | 日志记录、统计 |

## 示例插件

### PipelineDemo - 管线演示

**位置**：`PhiraMp.Plugins.PipelineDemo`

**功能**：
- 展示高优先级拦截器（Priority=10）
- 展示低优先级处理器（Priority=200）
- 展示如何使用 `IsHandled` 提前终止管线

**命令**：
- `/pipeline_test` - 触发高优先级拦截，演示提前返回
- `/pipeline_normal` - 正常通过管线，所有处理器都会执行

### DependencyInjectionDemo - 依赖注入演示

**位置**：`PhiraMp.Plugins.DependencyInjectionDemo`

**功能**：
- 提供玩家统计服务（服务提供者）
- 实现统计查询功能（服务消费者）
- 展示插件间通过服务接口协作

**命令**：
- `/stats [玩家名]` - 查看玩家统计
- `/stats_total` - 查看总体统计

## 文档

详细文档请参考：**PLUGIN_PIPELINE_AND_DI.md**

包含内容：
- 完整的 API 文档
- 详细的使用示例
- 最佳实践建议
- 常见问题解答
- 迁移指南

## 测试

运行测试脚本验证所有功能：
```bash
./test-new-features.sh
```

测试项目：
- ✅ 服务器构建
- ✅ 新插件构建
- ✅ 现有插件兼容性
- ✅ 插件部署

## 技术实现

### 新增文件

**核心系统**：
- `PhiraMp.Server/Plugins/PipelineSystem.cs` - 管线系统
- `PhiraMp.Server/Plugins/PluginServiceProvider.cs` - 依赖注入
- `PhiraMp.Server/Plugins/HandlerAdapters.cs` - 向后兼容

**示例插件**：
- `PhiraMp.Plugins.PipelineDemo/`
- `PhiraMp.Plugins.DependencyInjectionDemo/`

**文档**：
- `PLUGIN_PIPELINE_AND_DI.md` - 使用指南
- `test-new-features.sh` - 测试脚本
- `IMPLEMENTATION_SUMMARY.md` - 本文档

### 修改文件

**PluginContracts.cs**：
- 所有上下文类继承 `BasePipelineContext`
- `PluginContext` 添加 `ServiceProvider` 属性

**PluginManager.cs**：
- 集成 `PluginServiceProvider`
- 所有分发方法使用 `PipelineExecutor`
- 插件重载时清理服务

## 关键改进

1. **管线控制**
   - 插件可以完全控制事件处理流程
   - 支持优先级排序
   - 支持提前终止

2. **插件协作**
   - 通过服务接口实现插件间通信
   - 线程安全的服务容器
   - 清晰的依赖关系

3. **代码质量**
   - 完整的文档和示例
   - 自动化测试
   - 代码审查通过

4. **开发体验**
   - 简单易用的 API
   - 向后兼容
   - 清晰的最佳实践

## 使用场景

### 场景 1：安全过滤

```csharp
// 高优先级拦截敏感内容
[Export(typeof(IRoomMessageHandler))]
public class SecurityFilter : IRoomMessageHandler, IPrioritizedHandler
{
    public int Priority => 1;
    
    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        if (IsSensitive(context.Message))
        {
            context.IsHandled = true;  // 拦截
        }
    }
}
```

### 场景 2：命令路由

```csharp
// 中等优先级处理命令
[Export(typeof(IRoomMessageHandler))]
public class CommandRouter : IRoomMessageHandler, IPrioritizedHandler
{
    public int Priority => 50;
    
    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        if (context.Message.StartsWith("/"))
        {
            await HandleCommand(context);
            context.IsHandled = true;  // 命令已处理
        }
    }
}
```

### 场景 3：统计和日志

```csharp
// 低优先级记录所有消息
[Export(typeof(IRoomMessageHandler))]
public class MessageLogger : IRoomMessageHandler, IPrioritizedHandler
{
    public int Priority => 200;
    
    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        // 只有未被拦截的消息才会到这里
        LogMessage(context.Message);
    }
}
```

### 场景 4：跨插件功能

```csharp
// 插件 A 提供权限服务
[Export(typeof(IPluginModule))]
public class PermissionPlugin : IPluginModule
{
    public async Task InitializeAsync(PluginContext context)
    {
        context.ServiceProvider.RegisterService<IPermissionService>(_service);
    }
}

// 插件 B 使用权限服务
[Export(typeof(IRoomMessageHandler))]
public class AdminCommandPlugin : IPluginModule, IRoomMessageHandler
{
    private IPermissionService? _permissions;
    
    public async Task InitializeAsync(PluginContext context)
    {
        _permissions = context.ServiceProvider.GetService<IPermissionService>();
    }
    
    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        if (_permissions?.HasPermission(context.User, "admin") == true)
        {
            // 处理管理员命令
        }
    }
}
```

## 下一步

建议的改进方向：

1. **异步服务初始化**
   - 支持服务的异步初始化
   - 服务依赖解析

2. **服务生命周期**
   - 支持 Transient 和 Scoped
   - 自动释放资源

3. **插件间消息总线**
   - 发布-订阅模式
   - 事件驱动架构

4. **性能监控**
   - 管线执行时间统计
   - 服务调用追踪

## 总结

本次更新成功实现了用户要求的功能：

✅ **管线系统**：插件有权利彻底修改原始逻辑，可以使逻辑提前返回  
✅ **依赖注入**：允许了插件间的互相依赖

同时保持了：
- ✅ 向后兼容性
- ✅ 代码质量
- ✅ 文档完善
- ✅ 易于使用

插件系统现已升级为更加强大和灵活的架构！🎉
