using PhiraMp.Server.Console;
using PhiraMp.Server.Models;

namespace PhiraMp.Server;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Logger.Info("Phira 多人联机服务器（C#实现）");
        
        try
        {
            await RunServerAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "致命错误：");
            return 1;
        }
    }

    private static async Task RunServerAsync()
    {
        var config = ServerConfig.Load();

        Logger.Info("正在开启Phira多人联机服务器...");
        Logger.Info($"绑定IP: {config.BindIp}, 端口: {config.Port}, 房间最大玩家数: {config.RoomMaxPlayers}");

        using var server = new PhiraMpServer(config);
        var cts = server.GetCancellationTokenSource();

        // 注册内置控制台命令
        var commandSystem = server.GetState().ConsoleCommandSystem;
        if (commandSystem != null)
        {
            BuiltInCommands.RegisterAll(commandSystem, server);
        }

        System.Console.CancelKeyPress += (_, e) =>
        {
            Logger.Info("正在关闭...");
            e.Cancel = true;
            cts.Cancel();
        };

        // 启动服务器（会在内部启动命令系统）
        var serverTask = server.StartAsync();

        await Task.WhenAny(serverTask, Task.Delay(Timeout.Infinite, cts.Token));

        // 停止控制台命令监听
        commandSystem?.Stop();

        Logger.Info("服务器已终止");
    }
}
