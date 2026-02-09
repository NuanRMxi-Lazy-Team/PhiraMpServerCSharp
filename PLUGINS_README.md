# PhiraMP Server Plugin System

## 插件系统概述

PhiraMP Server 现已支持通过插件扩展功能，具有以下特性：

- ✅ **热加载 (Hot Loading)** - 运行时动态加载插件
- ✅ **热卸载 (Hot Unloading)** - 运行时卸载插件
- ✅ **热重载 (Hot Reloading)** - 自动检测并重新加载修改的插件
- ✅ **隔离性 (Isolation)** - 每个插件独立加载，避免冲突
- ✅ **事件驱动 (Event-Driven)** - 监听房间消息、状态变化等事件

## 快速开始

### 1. 构建和部署插件

```bash
# 构建整个解决方案
dotnet build PhiraMpCSharp.sln

# 部署插件到服务器
./deploy-plugins.sh    # Linux/Mac
# 或
deploy-plugins.bat     # Windows
```

### 2. 运行服务器

```bash
cd PhiraMp.Server/bin/Debug/net10.0
dotnet PhiraMp.Server.dll
```

服务器启动时会自动加载 `plugins` 目录中的所有插件。

### 3. 测试插件功能

运行测试脚本验证插件系统：

```bash
./test-plugins.sh      # Linux/Mac
```

## 内置示例插件

### CommandPlugin - 命令处理插件

提供游戏内命令系统，支持以下命令：

- `/help` - 显示可用命令列表
- `/kick <username>` - 踢出指定用户（仅房主可用）

**使用示例：**
1. 在游戏房间内发送 `/help`
2. 房主发送 `/kick PlayerName` 踢出玩家

### CycleVotingPlugin - 循环投票增强插件

为循环投票模式提供增强功能：

- 自动提示投票模式状态
- 统计投票轮数
- 提供投票帮助信息

**功能：**
- 当房间进入选歌阶段时，自动提示玩家可以投票
- 记录每个房间的投票统计数据
- 响应玩家的投票相关问题

## 插件热重载测试

插件系统支持在服务器运行时动态更新插件：

```bash
# 1. 启动服务器
cd PhiraMp.Server/bin/Debug/net10.0
dotnet PhiraMp.Server.dll

# 2. 在另一个终端修改插件并重新构建
cd PhiraMp.Plugins.CommandPlugin
# 修改代码...
dotnet build

# 3. 复制新的 DLL 到 plugins 目录
cp bin/Debug/net10.0/PhiraMp.Plugins.CommandPlugin.dll \
   ../../PhiraMp.Server/bin/Debug/net10.0/plugins/

# 4. 插件会在 1 秒内自动重新加载
```

服务器日志会显示：
```
[INFO] Plugin file changed: PhiraMp.Plugins.CommandPlugin.dll
[INFO] Reloading plugin: CommandPlugin
[INFO] Plugin unloaded: CommandPlugin
[INFO] Loading plugin: PhiraMp.Plugins.CommandPlugin.dll
[INFO] Plugin loaded: CommandPlugin v1.0.0
```

## 开发自己的插件

详细的插件开发指南请参阅 [PLUGIN_DEVELOPMENT.md](PLUGIN_DEVELOPMENT.md)。

### 简单示例

```csharp
using PhiraMp.Plugin.SDK;

[Plugin("MyPlugin", "1.0.0", "Your Name", "My awesome plugin")]
public class MyPlugin : PluginBase
{
    public override string Name => "MyPlugin";
    public override string Version => "1.0.0";

    public override async Task OnLoadAsync(IPluginContext context)
    {
        await base.OnLoadAsync(context);
        
        // 订阅房间消息事件
        context.ServerAPI.SubscribeToRoomMessages(OnRoomMessage);
        
        context.Logger.Info("MyPlugin loaded!");
    }

    private async Task OnRoomMessage(IRoomContext room, IUserContext user, string message)
    {
        if (message == "/hello")
        {
            await room.SendMessageToUserAsync(user, "Hello from MyPlugin!");
        }
    }
}
```

## 目录结构

```
PhiraMpServerCSharp/
├── PhiraMp.Plugin.SDK/           # 插件开发 SDK
│   ├── IPlugin.cs                # 插件接口
│   └── IPluginContext.cs         # 插件上下文接口
├── PhiraMp.Server/
│   └── Plugins/                  # 插件系统实现
│       ├── PluginManager.cs      # 插件管理器
│       ├── PluginContext.cs      # 插件上下文实现
│       └── ServerAPIImpl.cs      # 服务器 API 实现
├── PhiraMp.Plugins.CommandPlugin/    # 命令插件示例
├── PhiraMp.Plugins.CycleVoting/      # 循环投票插件示例
├── deploy-plugins.sh             # 插件部署脚本 (Linux/Mac)
├── deploy-plugins.bat            # 插件部署脚本 (Windows)
├── test-plugins.sh               # 插件测试脚本
└── PLUGIN_DEVELOPMENT.md         # 插件开发详细文档
```

## 技术实现

### 插件隔离

每个插件在独立的 `AssemblyLoadContext` 中加载，实现以下特性：

- **隔离性**: 插件可以使用不同版本的第三方库而不冲突
- **可卸载**: 插件可以被完全卸载并释放内存
- **共享核心**: SDK 和 Core 程序集被共享以减少内存占用

### 事件系统

插件通过订阅事件来响应服务器行为：

- `OnRoomMessage` - 房间消息事件
- `OnRoomStateChange` - 房间状态变化事件
- `OnUserJoin` - 用户加入房间事件
- `OnUserLeave` - 用户离开房间事件

### 热重载机制

使用 `FileSystemWatcher` 监视 plugins 目录：

- 检测 DLL 文件的创建和修改
- 1 秒延迟以确保文件完全写入
- 自动卸载旧版本并加载新版本

## 注意事项

1. **客户端协议**: 插件不能修改客户端协议，只能使用现有的服务器端功能
2. **性能影响**: 插件代码应该高效，避免阻塞操作
3. **错误处理**: 插件应妥善处理异常，避免影响服务器稳定性
4. **资源清理**: 插件卸载时必须清理所有资源

## 故障排查

### 插件未加载

检查：
- DLL 文件是否在 `plugins` 目录下
- 查看服务器启动日志
- 确认插件实现了 `IPlugin` 接口

### 热重载失败

检查：
- 文件是否被占用
- 等待 1-2 秒后重试
- 查看服务器日志中的错误信息

## 贡献

欢迎贡献新插件或改进现有插件！请参阅 [PLUGIN_DEVELOPMENT.md](PLUGIN_DEVELOPMENT.md) 了解详细信息。

## 许可证

与主项目相同的许可证。
