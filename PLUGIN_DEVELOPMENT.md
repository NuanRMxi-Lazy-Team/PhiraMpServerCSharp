# PhiraMP Server Plugin Development Guide

## 概述 (Overview)

PhiraMP Server 支持通过插件系统进行功能扩展。插件系统具有以下特性：

- **热加载 (Hot Loading)**: 服务器运行时可以加载新插件
- **热卸载 (Hot Unloading)**: 服务器运行时可以卸载插件
- **热重载 (Hot Reloading)**: 修改插件后自动重新加载
- **隔离性 (Isolation)**: 每个插件运行在独立的程序集加载上下文中，避免冲突
- **事件驱动 (Event-Driven)**: 插件通过订阅服务器事件来响应游戏中的各种行为

## 插件架构 (Architecture)

### 核心组件

1. **PhiraMp.Plugin.SDK** - 插件开发 SDK，包含所有插件接口和基类
2. **PluginManager** - 插件管理器，负责加载、卸载和管理插件生命周期
3. **ServerAPI** - 服务器 API，为插件提供访问服务器功能的接口
4. **PluginLoadContext** - 插件加载上下文，实现程序集隔离

### 插件生命周期

```
加载 (Load) -> 启用 (Enable) -> 运行中 (Running) -> 禁用 (Disable) -> 卸载 (Unload)
```

## 快速开始 (Quick Start)

### 1. 创建插件项目

```bash
dotnet new classlib -n MyPlugin -f net10.0
cd MyPlugin
dotnet add reference ../PhiraMp.Plugin.SDK/PhiraMp.Plugin.SDK.csproj
```

### 2. 配置项目文件

在 `.csproj` 中添加以下配置：

```xml
<PropertyGroup>
  <EnableDynamicLoading>true</EnableDynamicLoading>
</PropertyGroup>

<ItemGroup>
  <ProjectReference Include="..\PhiraMp.Plugin.SDK\PhiraMp.Plugin.SDK.csproj">
    <Private>false</Private>
    <ExcludeAssets>runtime</ExcludeAssets>
  </ProjectReference>
</ItemGroup>
```

### 3. 实现插件类

```csharp
using PhiraMp.Plugin.SDK;

namespace MyPlugin;

[Plugin("MyPlugin", "1.0.0", "Your Name", "Plugin description")]
public class MyPlugin : PluginBase
{
    public override string Name => "MyPlugin";
    public override string Version => "1.0.0";

    public override async Task OnLoadAsync(IPluginContext context)
    {
        await base.OnLoadAsync(context);
        
        // Subscribe to events
        context.ServerAPI.SubscribeToRoomMessages(OnRoomMessage);
        
        context.Logger.Info("MyPlugin loaded!");
    }

    public override Task OnUnloadAsync()
    {
        Context.Logger.Info("MyPlugin unloading...");
        return Task.CompletedTask;
    }

    private async Task OnRoomMessage(IRoomContext room, IUserContext user, string message)
    {
        // Handle room messages
        if (message == "/hello")
        {
            await room.SendMessageToUserAsync(user, "Hello from MyPlugin!");
        }
    }
}
```

### 4. 构建和部署

```bash
# 构建插件
dotnet build MyPlugin.csproj

# 复制 DLL 到服务器的 plugins 目录
cp bin/Debug/net10.0/MyPlugin.dll ../PhiraMp.Server/bin/Debug/net10.0/plugins/

# 或使用部署脚本
cd ..
./deploy-plugins.sh  # Linux/Mac
# 或
deploy-plugins.bat   # Windows
```

## API 参考 (API Reference)

### IPluginContext

插件上下文，提供对服务器 API 和日志记录的访问。

```csharp
public interface IPluginContext
{
    IPluginLogger Logger { get; }
    IServerAPI ServerAPI { get; }
    string ConfigDirectory { get; }  // 插件配置目录
    string DataDirectory { get; }    // 插件数据目录
}
```

### IServerAPI

服务器 API，提供订阅事件和访问房间的功能。

#### 事件订阅

```csharp
// 订阅房间消息事件
void SubscribeToRoomMessages(Func<IRoomContext, IUserContext, string, Task> handler);

// 订阅房间状态变化事件
void SubscribeToRoomStateChange(Func<IRoomContext, string, Task> handler);

// 订阅用户加入房间事件
void SubscribeToUserJoin(Func<IRoomContext, IUserContext, Task> handler);

// 订阅用户离开房间事件
void SubscribeToUserLeave(Func<IRoomContext, IUserContext, Task> handler);
```

#### 房间访问

```csharp
// 获取所有房间
IEnumerable<IRoomContext> GetRooms();

// 通过 ID 获取房间
IRoomContext? GetRoom(string roomId);
```

### IRoomContext

房间上下文，提供房间信息和操作。

```csharp
public interface IRoomContext
{
    string RoomId { get; }
    bool IsLocked { get; }
    bool IsCycleMode { get; }
    bool IsCycleVotingMode { get; }
    IUserContext? Host { get; }
    
    // 获取房间中的所有用户
    IEnumerable<IUserContext> GetUsers();
    
    // 向房间发送消息（所有用户可见）
    Task SendMessageAsync(string message);
    
    // 向特定用户发送私有消息
    Task SendMessageToUserAsync(IUserContext user, string message);
    
    // 踢出用户
    Task KickUserAsync(IUserContext user);
}
```

### IUserContext

用户上下文，提供用户信息。

```csharp
public interface IUserContext
{
    int UserId { get; }
    string UserName { get; }
    bool IsHost { get; }
    bool IsMonitor { get; }
}
```

## 示例插件 (Example Plugins)

### 1. CommandPlugin - 命令处理插件

实现了命令系统，支持 `/kick` 命令等。

**功能：**
- 解析以 `/` 开头的命令
- `/kick <username>` - 房主可以踢出玩家
- `/help` - 显示帮助信息

**源代码位置：** `PhiraMp.Plugins.CommandPlugin/CommandPlugin.cs`

### 2. CycleVotingPlugin - 循环投票增强插件

为循环投票模式提供增强功能。

**功能：**
- 在投票开始时发送提示消息
- 统计投票轮数
- 提供投票相关的帮助信息

**源代码位置：** `PhiraMp.Plugins.CycleVoting/CycleVotingPlugin.cs`

## 最佳实践 (Best Practices)

### 1. 错误处理

始终在事件处理器中捕获异常，避免影响其他插件：

```csharp
private async Task OnRoomMessage(IRoomContext room, IUserContext user, string message)
{
    try
    {
        // Your code here
    }
    catch (Exception ex)
    {
        Context.Logger.Error(ex, "Error handling message:");
    }
}
```

### 2. 资源清理

在 `OnUnloadAsync` 中清理资源：

```csharp
public override Task OnUnloadAsync()
{
    // 清理定时器、缓存等资源
    _timer?.Dispose();
    _cache.Clear();
    
    Context.Logger.Info("Resources cleaned up");
    return Task.CompletedTask;
}
```

### 3. 配置管理

使用 `ConfigDirectory` 保存插件配置：

```csharp
public override async Task OnLoadAsync(IPluginContext context)
{
    await base.OnLoadAsync(context);
    
    var configPath = Path.Combine(Context.ConfigDirectory, "config.json");
    if (File.Exists(configPath))
    {
        _config = JsonSerializer.Deserialize<MyConfig>(
            await File.ReadAllTextAsync(configPath));
    }
}
```

### 4. 性能考虑

- 避免在事件处理器中执行耗时操作
- 使用异步操作而不是阻塞调用
- 合理使用缓存减少重复计算

## 热重载 (Hot Reload)

服务器启动后会自动监视 `plugins` 目录：

1. **添加新插件**: 将 DLL 文件复制到 `plugins` 目录，插件会自动加载
2. **更新插件**: 覆盖现有 DLL 文件，插件会自动重新加载
3. **删除插件**: 需要手动停止服务器后删除

**注意**: 热重载有 1 秒的延迟，以确保文件完全写入。

## 调试技巧 (Debugging Tips)

### 1. 启用详细日志

在插件中使用 `Context.Logger.Debug()` 输出调试信息。

### 2. 附加调试器

可以在 Visual Studio 中附加到服务器进程进行调试：

1. 启动服务器
2. Debug -> Attach to Process
3. 选择 `PhiraMp.Server` 进程
4. 在插件代码中设置断点

### 3. 测试隔离

每个插件在独立的 AssemblyLoadContext 中运行，可以安全地测试不同版本的依赖项。

## 限制和约束 (Limitations)

1. **不能修改客户端协议**: 插件只能使用现有的服务器端功能，不能添加新的网络命令
2. **共享程序集**: 某些核心程序集（如 PhiraMp.Core、System.* 等）会被共享，不能使用不同版本
3. **性能影响**: 过多的插件或低效的插件代码可能影响服务器性能

## 故障排查 (Troubleshooting)

### 插件未加载

检查：
1. DLL 文件是否在正确的 `plugins` 目录下
2. 插件类是否实现了 `IPlugin` 接口
3. 查看服务器日志中的错误信息

### 插件冲突

如果两个插件冲突：
1. 检查是否使用了相同的第三方库的不同版本
2. 尝试逐个加载插件定位问题
3. 查看插件日志输出

### 热重载失败

如果热重载失败：
1. 确保没有其他进程占用 DLL 文件
2. 等待 1-2 秒后再次尝试
3. 检查文件权限

## 更多资源 (Resources)

- 源代码: `PhiraMp.Plugin.SDK/`
- 示例插件: `PhiraMp.Plugins.CommandPlugin/`, `PhiraMp.Plugins.CycleVoting/`
- 服务器代码: `PhiraMp.Server/Plugins/`

## 贡献 (Contributing)

欢迎贡献新的插件或改进现有插件！请确保：
1. 遵循代码风格
2. 添加适当的错误处理
3. 编写清晰的文档
4. 测试插件的加载、卸载和重载功能
