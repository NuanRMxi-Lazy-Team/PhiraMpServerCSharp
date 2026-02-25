using System.ComponentModel.Composition;
using PhiraMp.Server.Console;
using PhiraMp.Server.Plugins;

namespace PhiraMp.Plugins.ConsoleDemo;

/// <summary>
/// 演示插件如何注册自定义控制台命令
/// </summary>
[Export(typeof(IPluginModule))]
[Export(typeof(IConsoleCommandHandler))]
public class ConsoleCommandDemoPlugin : IPluginModule, IConsoleCommandHandler
{
    private PluginContext? _context;

    public Task InitializeAsync(PluginContext context)
    {
        _context = context;
        context.Logger.Info("控制台命令演示插件已初始化");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _context?.Logger.Info("控制台命令演示插件已关闭");
        return Task.CompletedTask;
    }

    public void RegisterCommands(ConsoleCommandSystem commandSystem)
    {
        // 示例命令 1: 显示插件信息
        commandSystem.RegisterCommand(new ConsoleCommand(
            "plugin-info",
            "显示插件信息",
            "plugin-info",
            async _ =>
            {
                _context?.Logger.Info("=== 插件信息 ===");
                _context?.Logger.Info("名称: ConsoleCommandDemoPlugin");
                _context?.Logger.Info("版本: 1.0.0");
                _context?.Logger.Info("描述: 演示如何注册自定义控制台命令");
                await Task.CompletedTask;
            }
        ));

        // 示例命令 2: 带参数的命令
        commandSystem.RegisterCommand(new ConsoleCommand(
            "echo",
            "回显输入的文本",
            "echo <文本>",
            async args =>
            {
                if (args.Length == 0)
                {
                    _context?.Logger.Warning("用法: echo <文本>");
                    return;
                }

                var message = string.Join(" ", args);
                _context?.Logger.Info($"回显: {message}");
                await Task.CompletedTask;
            }
        ));

        // 示例命令 3: 显示房间详细信息
        commandSystem.RegisterCommand(new ConsoleCommand(
            "room-detail",
            "显示指定房间的详细信息",
            "room-detail <房间ID>",
            async args =>
            {
                if (args.Length == 0)
                {
                    _context?.Logger.Warning("用法: room-detail <房间ID>");
                    return;
                }

                var roomId = args[0];
                var rooms = _context?.ServerState.Rooms;
                
                if (rooms != null && rooms.TryGetValue(roomId, out var room))
                {
                    _context?.Logger.Info($"=== 房间详情: {roomId} ===");
                    _context?.Logger.Info($"房主: {room.Host.Name} (ID: {room.Host.Id.ToString()})");
                    _context?.Logger.Info($"状态: {room.GetClientRoomState().State}");
                    _context?.Logger.Info($"直播模式: {room.Live}");
                    _context?.Logger.Info($"锁定: {room.Locked}");
                    _context?.Logger.Info($"循环模式: {room.Cycle}");
                    
                    var users = room.GetAllUsers();
                    _context?.Logger.Info($"玩家数量: {users.Count.ToString()}");
                    foreach (var user in users)
                    {
                        _context?.Logger.Info($"  - {user.Name} (ID: {user.Id.ToString()})");
                    }
                }
                else
                {
                    _context?.Logger.Warning($"未找到房间: {roomId}");
                }

                await Task.CompletedTask;
            }
        ));

        _context?.Logger.Info("已注册示例控制台命令: plugin-info, echo, room-detail");
    }
}

