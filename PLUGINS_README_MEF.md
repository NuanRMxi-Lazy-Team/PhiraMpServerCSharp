# PhiraMP Server 插件系统 (MEF 版本)

## 概述

PhiraMP Server 现使用 **MEF (Managed Extensibility Framework)** - .NET 标准插件框架，提供最大的灵活性和可扩展性。

### 核心优势

- ✅ **无 SDK 限制** - 插件直接引用服务器 DLL，无需额外的 SDK
- ✅ **最大灵活性** - 插件可以直接访问和修改服务器的所有组件
- ✅ **行业标准** - 使用 .NET MEF 框架，成熟稳定
- ✅ **热重载** - 运行时动态加载、卸载、重新加载插件
- ✅ **多契约导出** - 一个插件可以实现多个接口

## 快速开始

### 1. 构建和部署插件

```bash
# 先构建服务器
cd PhiraMp.Server
dotnet build

# 再构建插件
cd ../PhiraMp.Plugins.CommandPlugin
dotnet build

# 部署所有插件
cd ..
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

### 3. 验证插件加载

服务器日志会显示：
```
[INFO] Loading plugins from .../plugins
[INFO] Cataloged plugin: PhiraMp.Plugins.CommandPlugin.dll
[CommandPlugin] Initialized with MEF - No SDK required!
[INFO] Loaded 2 plugins with MEF
  - 2 modules
  - 2 message handlers
  - 1 state handlers
```

## 内置插件

### CommandPlugin - 命令处理插件

**功能：**
- `/help` - 显示可用命令列表
- `/kick <username>` - 踢出指定用户（仅房主可用）
- `/info` - 显示插件信息

**使用示例：**
1. 在游戏房间内发送 `/help`
2. 房主发送 `/kick PlayerName` 踢出玩家

**技术特点：**
- 使用 MEF `[Export]` 属性
- 直接访问 `Room` 和 `User` 对象
- 无 SDK 依赖

### CycleVotingPlugin - 循环投票增强插件

**功能：**
- 当房间进入选歌阶段时，自动提示玩家可以投票
- 记录每个房间的投票统计数据
- 响应玩家的投票相关问题

**技术特点：**
- 导出多个契约：`IPluginModule`, `IRoomStateHandler`, `IRoomMessageHandler`
- 直接访问 `ServerState` 查看所有房间

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
   ../PhiraMp.Server/bin/Debug/net10.0/plugins/

# 4. 插件会在 1 秒内自动重新加载
```

服务器日志会显示：
```
[INFO] Plugin file changed: PhiraMp.Plugins.CommandPlugin.dll
[INFO] Reloading all plugins...
[CommandPlugin] Shutting down
[CommandPlugin] Initialized with MEF - No SDK required!
[INFO] Loaded 2 plugins with MEF
```

## 开发自己的插件

### 简单示例

```csharp
using System.ComponentModel.Composition;
using PhiraMp.Server.Plugins;

[Export(typeof(IPluginModule))]
[Export(typeof(IRoomMessageHandler))]
public class MyPlugin : IPluginModule, IRoomMessageHandler
{
    public async Task InitializeAsync(PluginContext context)
    {
        // 直接访问服务器状态！
        Console.WriteLine($"Server has {context.ServerState.Rooms.Count} rooms");
    }
    
    public Task ShutdownAsync()
    {
        Console.WriteLine("MyPlugin shutting down");
        return Task.CompletedTask;
    }
    
    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        // 直接访问 Room 和 User 对象
        if (context.Message == "/hello")
        {
            await context.Room.SendAsync(
                new Core.ChatMessage(-1, $"Hello {context.User.Name}!"));
        }
    }
}
```

### 详细开发指南

查看 [PLUGIN_DEVELOPMENT_MEF.md](PLUGIN_DEVELOPMENT_MEF.md) 了解：
- 完整的开发流程
- 所有可用的契约接口
- 高级特性和技巧
- 错误处理和调试

## MEF vs 旧方式对比

| 特性 | 旧 SDK 方式 | MEF 方式 |
|------|-------------|----------|
| **SDK 依赖** | ❌ 需要引用 SDK | ✅ 无需 SDK |
| **灵活性** | ❌ 受限于 SDK API | ✅ 完全控制 |
| **服务器访问** | ❌ 通过包装接口 | ✅ 直接访问 |
| **标准化** | ❌ 自定义系统 | ✅ .NET MEF 标准 |
| **多契约** | ❌ 单一模式 | ✅ 多契约导出 |

## 目录结构

```
PhiraMpServerCSharp/
├── PhiraMp.Server/
│   ├── Plugins/
│   │   ├── PluginManager.cs        # MEF 插件管理器
│   │   └── PluginContracts.cs      # 可选契约接口
│   └── bin/Debug/net10.0/
│       └── plugins/                 # 插件目录
├── PhiraMp.Plugins.CommandPlugin/   # 命令插件示例
├── PhiraMp.Plugins.CycleVoting/     # 投票插件示例
├── deploy-plugins.sh/bat            # 插件部署脚本
└── PLUGIN_DEVELOPMENT_MEF.md        # 开发文档
```

## 技术实现

### MEF 组合

- **AggregateCatalog** - 聚合多个插件程序集
- **AssemblyCatalog** - 从程序集中发现导出
- **CompositionContainer** - 管理组合和生命周期
- **[Export]** - 标记导出的类型
- **[ImportMany]** - 导入多个实例

### 插件发现

```csharp
// 服务器端
[ImportMany(typeof(IRoomMessageHandler))]
public IEnumerable<IRoomMessageHandler>? MessageHandlers { get; set; }

// 插件端
[Export(typeof(IRoomMessageHandler))]
public class MyPlugin : IRoomMessageHandler { }
```

## 故障排查

### 插件未加载

**检查：**
- DLL 文件是否在 `plugins` 目录下
- 是否使用了 `[Export]` 属性
- 查看服务器启动日志
- 确认先构建服务器，再构建插件

### 引用错误

**解决：**
```bash
# 按正确顺序构建
cd PhiraMp.Server
dotnet build

cd ../MyPlugin
dotnet build
```

### 热重载失败

**检查：**
- 文件是否被占用
- 等待 1-2 秒后重试
- 查看服务器日志中的错误信息

## 注意事项

1. **客户端协议** - 插件不能修改客户端协议，只能使用现有的服务器端功能
2. **性能影响** - 插件代码应该高效，避免阻塞操作
3. **错误处理** - 插件应妥善处理异常，避免影响服务器稳定性
4. **资源清理** - 插件卸载时必须清理所有资源

## 贡献

欢迎贡献新插件或改进现有插件！

1. Fork 本项目
2. 创建你的插件
3. 提交 Pull Request
4. 确保插件使用 MEF 标准
5. 提供清晰的文档

## 更多资源

- **开发指南**: [PLUGIN_DEVELOPMENT_MEF.md](PLUGIN_DEVELOPMENT_MEF.md)
- **MEF 官方文档**: https://docs.microsoft.com/en-us/dotnet/framework/mef/
- **示例代码**: `PhiraMp.Plugins.CommandPlugin/`, `PhiraMp.Plugins.CycleVoting/`

## 许可证

与主项目相同的许可证。

---

**🎉 使用 MEF 的新插件系统 - 更灵活，更强大，更标准化！**
