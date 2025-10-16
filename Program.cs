using System;
using System.Threading;
using System.Threading.Tasks;
using PhiraMpServer.Common;
using PhiraMpServer.Server;

namespace PhiraMpServer;

class Program
{
    static async Task<int> Main(string[] args)
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

    static async Task RunServerAsync()
    {
        var config = ServerConfig.Load();

        Logger.Info("Starting Phira Multiplayer Server");
        Logger.Info($"Bind IP: {config.BindIp}, Port: {config.Port}, Room Max Players: {config.RoomMaxPlayers}");
        Logger.Info("Press Ctrl+C to stop the server");

        using var server = new Server.PhiraMpServer(config);

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
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
