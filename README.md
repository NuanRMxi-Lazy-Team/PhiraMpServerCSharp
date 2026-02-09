# PhiraMpServerCSharp
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FNuanRMxi-Lazy-Team%2FPhiraMpServerCSharp.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2FNuanRMxi-Lazy-Team%2FPhiraMpServerCSharp?ref=badge_shield)

PhiraMpServerCSharp 是一个用 C# 重写的 [phira-mp](https://github.com/teamflos/phira-mp) 服务端！

## 对原版的兼容状况
完全兼容，AI的大力出奇迹！

## 运行环境
- .NET 10.0 或更高版本

## 使用方法
0. 确保你有dotnet环境。
1. 克隆或下载本仓库。
2. dotnet run
3. 服务器启动成功，也可以修改 server_config.yml 来配置服务器。

或者：
1. 从 Releases 下载编译好的自包含版本。
2. 直接运行。

## 特色功能？
- **MEF 插件系统** - 使用 .NET 标准 MEF 框架实现插件功能
  - 你问我怎么开发？哈哈我不到啊，你看看示例代码就知道了

## 项目内提供插件
- CycleVoting：魔改循环模式，将循环模式改为投票模式
- RandomRoom：允许玩家使用特定保留房间号加入随机房间
- SinglePlayerPrevention：阻止只有一个人的房间开始游戏

## 警告？
- 仓库里有很多没有经过验证的vibecode留下的脚本，自己看看啥内容再运行...
- 插件理论上把插件dll丢进插件目录就能加载，甚至可以热加载吼
- 啊什么？版本兼容？没做，我也不确定有没有，嘻嘻嘻嘻嘻嘻

## License
[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2FNuanRMxi-Lazy-Team%2FPhiraMpServerCSharp.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2FNuanRMxi-Lazy-Team%2FPhiraMpServerCSharp?ref=badge_large)