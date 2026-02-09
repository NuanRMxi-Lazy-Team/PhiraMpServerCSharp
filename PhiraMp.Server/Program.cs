namespace PhiraMp.Server;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Logger.Info("Phira Multiplayer Server (C# Implementation)");
        
        try
        {
            await RunServerAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fatal error occurred:");
            return 1;
        }
    }

    private static async Task RunServerAsync()
    {
        var config = ServerConfig.Load();

        Logger.Info("正在开启Phira多人联机服务器...");
        Logger.Info($"绑定IP: {config.BindIp}, 端口: {config.Port}, 房间最大玩家数: {config.RoomMaxPlayers}");
        Logger.Info("使用Ctrl+C可以安全地关闭服务器");

        using var server = new PhiraMpServer(config);

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            Logger.Info("Shutting down...");
            e.Cancel = true;
            cts.Cancel();
        };

        var serverTask = server.StartAsync();

        await Task.WhenAny(serverTask, Task.Delay(Timeout.Infinite, cts.Token));

        Logger.Info("Server stopped");
    }
}
