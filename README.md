# PhiraMpServerCSharp
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FNuanRMxi-Lazy-Team%2FPhiraMpServerCSharp.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2FNuanRMxi-Lazy-Team%2FPhiraMpServerCSharp?ref=badge_shield)

PhiraMpServerCSharp 是一个用 C# 重写的 [phira-mp](https://github.com/teamflos/phira-mp) 服务端，意义不明，由AI生成。  

## 对原版的兼容状况
完全兼容，顺带解决了玩家一个人就想开始游戏的问题👍。

## 运行环境
- .NET 10.0 或更高版本

## 使用方法
0. 确保你有dotnet环境。
1. 克隆或下载本仓库。
2. dotnet run
3. 服务器启动成功，也可以修改 server_config.yml 来配置服务器。
或从 Releases 下载编译好的版本。

## 特色功能
- 不支持单人游戏
- 支持投票功能，魔改了循环模式，原版客户端可兼容，行为详见[配置文件](https://github.com/NuanRMxi-Lazy-Team/PhiraMpServerCSharp/blob/main/server_config.example.yml),此功能需要手动开启。
- **🔌 插件系统** - 支持动态加载插件，实现功能扩展
  - ✅ 热加载/热卸载/热重载
  - ✅ 插件隔离，避免冲突
  - ✅ 事件驱动架构
  - 📖 详细文档: [插件使用说明](PLUGINS_README.md) | [插件开发指南](PLUGIN_DEVELOPMENT.md)

## 内置插件

### CommandPlugin - 命令处理插件
提供游戏内命令功能：
- `/help` - 显示帮助信息
- `/kick <username>` - 踢出玩家（房主专用）

### CycleVotingPlugin - 循环投票增强插件
为循环投票模式提供增强功能，包括自动提示和统计。

## 快速开始

### 构建和运行
```bash
# 构建项目
dotnet build PhiraMpCSharp.sln

# 部署插件
./deploy-plugins.sh  # Linux/Mac
# 或
deploy-plugins.bat   # Windows

# 运行服务器
cd PhiraMp.Server/bin/Debug/net10.0
dotnet PhiraMp.Server.dll
```

### 开发自己的插件
查看 [PLUGIN_DEVELOPMENT.md](PLUGIN_DEVELOPMENT.md) 了解如何开发插件。

## License
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FNuanRMxi-Lazy-Team%2FPhiraMpServerCSharp.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2FNuanRMxi-Lazy-Team%2FPhiraMpServerCSharp?ref=badge_large)