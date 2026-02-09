# PhiraMP Server Plugin Development Guide (MEF-Based)

## 概述 (Overview)

PhiraMP Server 使用 **MEF (Managed Extensibility Framework)** 实现插件系统。这是 .NET 的行业标准插件框架，提供最大的灵活性。

**核心特性：**
- ✅ **无 SDK 要求** - 插件直接引用服务器 DLL
- ✅ **MEF 标准** - 使用 `[Export]` 属性进行发现
- ✅ **最大灵活性** - 插件可以直接访问和修改服务器组件
- ✅ **多契约导出** - 一个插件可以导出多个接口
- ✅ **热加载/卸载/重载** - 运行时动态管理
- ✅ **约定优于配置** - 无需强制继承基类

## 快速开始 (Quick Start)

### 1. 创建插件项目

```bash
dotnet new classlib -n MyPlugin -f net10.0
cd MyPlugin
```

### 2. 配置项目文件

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="System.ComponentModel.Composition" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- 直接引用服务器 DLL -->
    <Reference Include="PhiraMp.Server">
      <HintPath>..\PhiraMp.Server\bin\Debug\net10.0\PhiraMp.Server.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="PhiraMp.Core">
      <HintPath>..\PhiraMp.Core\bin\Debug\net10.0\PhiraMp.Core.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

### 3. 实现插件类

```csharp
using System.ComponentModel.Composition;
using PhiraMp.Server;
using PhiraMp.Server.Plugins;

namespace MyPlugin;

// 使用 [Export] 属性让 MEF 发现你的插件
[Export(typeof(IPluginModule))]
[Export(typeof(IRoomMessageHandler))]
public class MyPlugin : IPluginModule, IRoomMessageHandler
{
    private PluginContext? _context;

    // 初始化方法 - 可选实现
    public async Task InitializeAsync(PluginContext context)
    {
        _context = context;
        
        // 直接访问服务器状态！
        Console.WriteLine($"Server has {context.ServerState.Rooms.Count} rooms");
        
        await Task.CompletedTask;
    }

    // 关闭方法 - 可选实现
    public Task ShutdownAsync()
    {
        Console.WriteLine("MyPlugin shutting down");
        return Task.CompletedTask;
    }

    // 处理房间消息
    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        if (context.Message == "/hello")
        {
            // 直接访问 Room 和 User 对象！
            await context.Room.SendAsync(
                new Core.ChatMessage(-1, $"Hello {context.User.Name}!"));
        }
    }
}
```

### 4. 构建和部署

```bash
# 1. 先构建服务器
cd ../PhiraMp.Server
dotnet build

# 2. 再构建插件
cd ../MyPlugin
dotnet build

# 3. 复制 DLL 到 plugins 目录
cp bin/Debug/net10.0/MyPlugin.dll ../PhiraMp.Server/bin/Debug/net10.0/plugins/

# 4. 运行服务器
cd ../PhiraMp.Server/bin/Debug/net10.0
dotnet PhiraMp.Server.dll
```

## 可用契约接口 (Available Contracts)

### IPluginModule
插件生命周期管理（可选实现）

```csharp
[Export(typeof(IPluginModule))]
public class MyPlugin : IPluginModule
{
    Task InitializeAsync(PluginContext context);  // 加载时调用
    Task ShutdownAsync();                         // 卸载时调用
}
```

### IRoomMessageHandler
处理房间消息

```csharp
[Export(typeof(IRoomMessageHandler))]
public class MyPlugin : IRoomMessageHandler
{
    Task HandleMessageAsync(RoomMessageContext context);
}
```

### IRoomStateHandler
处理房间状态变化

```csharp
[Export(typeof(IRoomStateHandler))]
public class MyPlugin : IRoomStateHandler
{
    Task HandleStateChangeAsync(RoomStateContext context);
}
```

### IUserJoinHandler
处理用户加入事件

```csharp
[Export(typeof(IUserJoinHandler))]
public class MyPlugin : IUserJoinHandler
{
    Task HandleUserJoinAsync(UserEventContext context);
}
```

### IUserLeaveHandler
处理用户离开事件

```csharp
[Export(typeof(IUserLeaveHandler))]
public class MyPlugin : IUserLeaveHandler
{
    Task HandleUserLeaveAsync(UserEventContext context);
}
```

## 高级特性 (Advanced Features)

### 1. 多契约导出
一个插件可以导出多个接口：

```csharp
[Export(typeof(IPluginModule))]
[Export(typeof(IRoomMessageHandler))]
[Export(typeof(IRoomStateHandler))]
public class MyPlugin : IPluginModule, IRoomMessageHandler, IRoomStateHandler
{
    // 实现所有接口...
}
```

### 2. 直接访问服务器组件

```csharp
public async Task InitializeAsync(PluginContext context)
{
    // 访问服务器状态
    var serverState = context.ServerState;
    
    // 遍历所有房间
    foreach (var room in serverState.Rooms.Values)
    {
        Console.WriteLine($"Room: {room.Id}");
    }
    
    // 访问所有用户
    foreach (var user in serverState.Users.Values)
    {
        Console.WriteLine($"User: {user.Name}");
    }
}
```

### 3. 直接修改服务器行为

```csharp
public async Task HandleMessageAsync(RoomMessageContext context)
{
    // 完全控制房间对象
    var room = context.Room;
    var user = context.User;
    
    // 修改房间属性
    if (user.Name == "Admin")
    {
        room.Locked = false;  // 直接修改！
    }
    
    // 访问房间的所有方法
    await room.BroadcastAsync(new SomeCommand());
    
    // 踢人
    if (user.SessionRef != null && user.SessionRef.TryGetTarget(out var session))
    {
        session.Dispose();  // 直接关闭会话
    }
}
```

### 4. 自定义契约

你不需要使用预定义的接口！创建你自己的：

```csharp
// 定义你自己的契约
public interface IMyCustomBehavior
{
    void DoSomething();
}

// 导出你的契约
[Export(typeof(IMyCustomBehavior))]
public class MyPlugin : IMyCustomBehavior
{
    public void DoSomething()
    {
        Console.WriteLine("Custom behavior!");
    }
}
```

## 示例插件 (Example Plugins)

### CommandPlugin - 命令处理

完整源代码：`PhiraMp.Plugins.CommandPlugin/CommandPlugin.cs`

**特性：**
- 导出 `IPluginModule` 和 `IRoomMessageHandler`
- 实现 `/kick`, `/help`, `/info` 命令
- 无需 SDK - 直接引用服务器 DLL
- 完全控制房间和用户对象

### CycleVotingPlugin - 循环投票增强

完整源代码：`PhiraMp.Plugins.CycleVoting/CycleVotingPlugin.cs`

**特性：**
- 导出多个契约：`IPluginModule`, `IRoomStateHandler`, `IRoomMessageHandler`
- 直接访问 `ServerState` 查看所有房间
- 状态跟踪和统计
- 自动提示和帮助信息

## MEF vs 旧 SDK 对比

| 特性 | 旧 SDK 方式 | MEF 方式 |
|------|-------------|----------|
| **SDK 依赖** | ❌ 必须引用 SDK | ✅ 无需 SDK |
| **基类要求** | ❌ 必须继承 PluginBase | ✅ 无需继承 |
| **接口强制** | ❌ 必须实现 IPlugin | ✅ 可选实现 |
| **灵活性** | ❌ 受限于 SDK API | ✅ 完全控制 |
| **服务器访问** | ❌ 通过包装接口 | ✅ 直接访问 |
| **标准化** | ❌ 自定义系统 | ✅ .NET MEF 标准 |
| **多契约** | ❌ 单一模式 | ✅ 多契约导出 |

## 热重载 (Hot Reload)

服务器自动监视 `plugins` 目录：

```bash
# 修改插件代码
cd MyPlugin
# 编辑代码...

# 重新构建
dotnet build

# 复制到 plugins 目录
cp bin/Debug/net10.0/MyPlugin.dll ../PhiraMp.Server/bin/Debug/net10.0/plugins/

# 插件会在 1 秒内自动重新加载！
```

## 调试技巧 (Debugging)

### 1. 附加调试器

1. 启动服务器
2. Visual Studio: Debug -> Attach to Process
3. 选择 `dotnet` 进程
4. 在插件代码中设置断点

### 2. 输出调试信息

```csharp
public async Task HandleMessageAsync(RoomMessageContext context)
{
    Console.WriteLine($"[DEBUG] Message: {context.Message}");
    Console.WriteLine($"[DEBUG] User: {context.User.Name}");
    Console.WriteLine($"[DEBUG] Room: {context.Room.Id}");
}
```

## 最佳实践 (Best Practices)

### 1. 错误处理

始终捕获异常：

```csharp
public async Task HandleMessageAsync(RoomMessageContext context)
{
    try
    {
        // 你的代码
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] {ex.Message}");
    }
}
```

### 2. 资源清理

在 `ShutdownAsync` 中清理资源：

```csharp
public Task ShutdownAsync()
{
    _timer?.Dispose();
    _connections.Clear();
    return Task.CompletedTask;
}
```

### 3. 性能考虑

- 避免阻塞操作
- 使用异步方法
- 缓存重复计算的结果

## 故障排查 (Troubleshooting)

### 插件未加载

**检查：**
1. DLL 文件在 `plugins` 目录下吗？
2. 使用了 `[Export]` 属性吗？
3. 查看服务器日志中的错误信息
4. 确保先构建服务器，再构建插件

### 引用错误

**解决：**
```bash
# 1. 先构建服务器
cd PhiraMp.Server
dotnet build

# 2. 再构建插件
cd ../MyPlugin
dotnet build
```

### 热重载失败

**检查：**
1. 文件是否被占用？
2. 等待 1-2 秒后重试
3. 查看服务器日志

## 技术细节 (Technical Details)

### MEF 组合

服务器使用 `CompositionContainer` 进行插件发现：

```csharp
var catalog = new AggregateCatalog();
var assembly = Assembly.LoadFrom("plugin.dll");
catalog.Catalogs.Add(new AssemblyCatalog(assembly));

var container = new CompositionContainer(catalog);
container.SatisfyImportsOnce(this);  // 导入所有 [Export]
```

### 程序集隔离

虽然移除了 AssemblyLoadContext，但插件仍然是独立的程序集，可以：
- 使用不同版本的第三方库（通过 Private=false）
- 在热重载时卸载

## 更多资源 (Resources)

- **MEF 文档**: https://docs.microsoft.com/en-us/dotnet/framework/mef/
- **示例插件**: `PhiraMp.Plugins.CommandPlugin/`, `PhiraMp.Plugins.CycleVoting/`
- **服务器代码**: `PhiraMp.Server/Plugins/`

## 总结

使用 MEF 的新插件系统提供了：

✅ **最大灵活性** - 无 SDK 限制，直接访问服务器  
✅ **行业标准** - 使用 .NET MEF 框架  
✅ **简单易用** - 只需 `[Export]` 属性  
✅ **强大功能** - 完全控制服务器行为  
✅ **热重载** - 运行时动态管理  

开始创建你自己的插件吧！🚀
