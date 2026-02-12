namespace PhiraMp.Server.Console;

/// <summary>
/// 内置控制台命令
/// </summary>
public static class BuiltInCommands
{
    /// <summary>
    /// 注册所有内置命令
    /// </summary>
    public static void RegisterAll(ConsoleCommandSystem system, PhiraMpServer server)
    {
        // Help 命令
        system.RegisterCommand(new ConsoleCommand(
            "help",
            "显示所有可用的控制台命令",
            "help [命令名]",
            async args =>
            {
                if (args.Length > 0)
                {
                    // 显示特定命令的详细帮助
                    var cmdName = args[0].ToLower();
                    var commands = system.GetCommands();
                    if (commands.TryGetValue(cmdName, out var cmd))
                    {
                        System.Console.ForegroundColor = ConsoleColor.Cyan;
                        System.Console.WriteLine($"\n命令: {cmd.Name}");
                        System.Console.ResetColor();
                        System.Console.WriteLine($"描述: {cmd.Description}");
                        System.Console.WriteLine($"用法: {cmd.Usage}");
                        System.Console.WriteLine();
                    }
                    else
                    {
                        Logger.Warning($"未找到命令: {cmdName}");
                    }
                }
                else
                {
                    // 显示所有命令
                    System.Console.ForegroundColor = ConsoleColor.Cyan;
                    System.Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
                    System.Console.WriteLine("║              可用控制台命令                                 ║");
                    System.Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
                    System.Console.ResetColor();
                    
                    var commands = system.GetCommands();
                    var sortedCommands = commands.Values.OrderBy(c => c.Name).ToList();
                    
                    foreach (var cmd in sortedCommands)
                    {
                        System.Console.ForegroundColor = ConsoleColor.Yellow;
                        System.Console.Write($"  {cmd.Name.PadRight(20)}");
                        System.Console.ResetColor();
                        System.Console.WriteLine($" - {cmd.Description}");
                    }
                    
                    System.Console.WriteLine();
                    System.Console.ForegroundColor = ConsoleColor.Gray;
                    System.Console.WriteLine("提示: 使用 'help <命令名>' 查看命令的详细用法");
                    System.Console.WriteLine("      使用 TAB 键自动补全命令");
                    System.Console.WriteLine("      使用 ↑↓ 键浏览历史命令");
                    System.Console.ResetColor();
                    System.Console.WriteLine();
                }
                await Task.CompletedTask;
            }
        ));

        // Clear 命令
        system.RegisterCommand(new ConsoleCommand(
            "clear",
            "清屏",
            "clear",
            async _ =>
            {
                System.Console.Clear();
                Logger.Info("控制台已清屏");
                await Task.CompletedTask;
            }
        ));

        // Stop 命令
        system.RegisterCommand(new ConsoleCommand(
            "stop",
            "停止服务器",
            "stop",
            async _ =>
            {
                Logger.Info("正在停止服务器...");
                server.Stop();
                await Task.CompletedTask;
            }
        ));

        // Status 命令
        system.RegisterCommand(new ConsoleCommand(
            "status",
            "显示服务器状态信息",
            "status",
            async _ =>
            {
                var state = server.GetState();
                System.Console.ForegroundColor = ConsoleColor.Cyan;
                System.Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
                System.Console.WriteLine("║                 服务器状态                                  ║");
                System.Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
                System.Console.ResetColor();
                
                System.Console.WriteLine($"  活跃会话数: {state.Sessions.Count.ToString()}");
                System.Console.WriteLine($"  在线用户数: {state.Users.Count.ToString()}");
                System.Console.WriteLine($"  房间数量:   {state.Rooms.Count.ToString()}");
                
                if (state.Rooms.Count > 0)
                {
                    System.Console.WriteLine();
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    System.Console.WriteLine("  房间列表:");
                    System.Console.ResetColor();
                    foreach (var room in state.Rooms.Values)
                    {
                        var userCount = room.GetAllUsers().Count;
                        System.Console.WriteLine($"    • 房间 {room.Id.Value}: {userCount.ToString()} 玩家, 状态: {room.State.GetType().Name}");
                    }
                }
                System.Console.WriteLine();
                
                await Task.CompletedTask;
            }
        ));

        // List 命令
        system.RegisterCommand(new ConsoleCommand(
            "list",
            "列出在线用户",
            "list",
            async _ =>
            {
                var state = server.GetState();
                System.Console.ForegroundColor = ConsoleColor.Cyan;
                System.Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
                System.Console.WriteLine("║               在线用户列表                                  ║");
                System.Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
                System.Console.ResetColor();
                
                if (state.Users.Count == 0)
                {
                    System.Console.ForegroundColor = ConsoleColor.Gray;
                    System.Console.WriteLine("  (无在线用户)");
                    System.Console.ResetColor();
                }
                else
                {
                    foreach (var user in state.Users.Values.OrderBy(u => u.Id))
                    {
                        var roomInfo = user.Room != null ? $"房间: {user.Room.Id.Value}" : "大厅";
                        System.Console.ForegroundColor = ConsoleColor.Yellow;
                        System.Console.Write($"  [{user.Id.ToString()}]");
                        System.Console.ResetColor();
                        System.Console.WriteLine($" {user.Name} - {roomInfo}");
                    }
                }
                System.Console.WriteLine();
                
                await Task.CompletedTask;
            }
        ));

        // Kick 命令
        system.RegisterCommand(new ConsoleCommand(
            "kick",
            "踢出指定用户",
            "kick <用户ID>",
            async args =>
            {
                if (args.Length < 1)
                {
                    Logger.Warning("用法: kick <用户ID>");
                    return;
                }

                if (!int.TryParse(args[0], out int userId))
                {
                    Logger.Warning("无效的用户ID");
                    return;
                }

                var state = server.GetState();
                if (state.Users.TryGetValue(userId, out var user))
                {
                    Logger.Info($"正在踢出用户 {user.Name} (ID: {userId.ToString()})...");
                    
                    // Find and dispose the user's session
                    Session? userSession = null;
                    user.SessionRef?.TryGetTarget(out userSession);
                    
                    if (userSession != null && state.Sessions.TryGetValue(userSession.Id, out var session))
                    {
                        session.Dispose();
                        Logger.Info($"用户 {user.Name} 已被踢出");
                    }
                    else
                    {
                        Logger.Warning($"未找到用户 {user.Name} 的活跃会话");
                    }
                }
                else
                {
                    Logger.Warning($"未找到用户ID: {userId}");
                }

                await Task.CompletedTask;
            }
        ));

        // Reload 命令
        system.RegisterCommand(new ConsoleCommand(
            "reload",
            "重新加载所有插件",
            "reload",
            async _ =>
            {
                Logger.Info("正在重新加载插件...");
                var state = server.GetState();
                if (state.PluginManager != null)
                {
                    await state.PluginManager.ReloadPluginsAsync();
                    Logger.Info("插件重新加载完成");
                }
                else
                {
                    Logger.Warning("插件管理器未初始化");
                }
            }
        ));

        // Say 命令 (广播消息)
        system.RegisterCommand(new ConsoleCommand(
            "say",
            "向所有房间广播消息",
            "say <消息内容>",
            async args =>
            {
                if (args.Length < 1)
                {
                    Logger.Warning("用法: say <消息内容>");
                    return;
                }

                var message = string.Join(" ", args);
                var state = server.GetState();
                
                Logger.Info($"广播消息: {message}");
                
                int roomCount = 0;
                foreach (var room in state.Rooms.Values)
                {
                    await room.SendAsync(new PhiraMp.Core.ChatMessage(0, $"[服务器] {message}"));
                    roomCount++;
                }
                
                Logger.Info($"消息已发送到 {roomCount.ToString()} 个房间");
            }
        ));
    }
}





