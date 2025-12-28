using System;
using System.Threading;
using System.Threading.Tasks;
using PhiraMpServer.Common;
using PhiraMpServer.Server;

namespace PhiraMpServer;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Logger.Info("Phira Multiplayer Server (C# Implementation)");
        var config = ServerConfig.Load();
        var serverState = new ServerState(config);
        
        try
        {
            var externalInterfaceServer =
                config.EnableExternalInterface ? await RunExternalInterfaceServerAsync(config, serverState) : null;
            await RunServerAsync(config, serverState, externalInterfaceServer);
            return 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Fatal error occurred:");
            return 1;
        }
    }

    private static async Task RunServerAsync(ServerConfig config, ServerState serverState,
        PhiraMpServer.ExternalInterface.Server? externalInterfaceServer = null)
    {
        Logger.Info("Starting Phira Multiplayer Server");
        Logger.Info($"Bind IP: {config.BindIp}, Port: {config.Port}, Room Max Players: {config.RoomMaxPlayers}");
        Logger.Info("Press Ctrl+C to stop the server");

        using var server = new Server.PhiraMpServer(config, serverState, externalInterfaceServer);

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

    private static async Task<PhiraMpServer.ExternalInterface.Server> RunExternalInterfaceServerAsync(
        ServerConfig config, ServerState serverState)
    {
        // Create and configure dispatcher
        var dispatcher = new PhiraMpServer.ExternalInterface.Common.CommandDispatcher();
        
        // Register all command handlers (with validation)
        CommandHandlerConfiguration.RegisterAllHandlers(dispatcher, serverState, DateTime.Now);
        
        var externalInterfaceServer =
            new PhiraMpServer.ExternalInterface.Server(config.ExternalInterfaceIp, config.ExternalInterfacePort, dispatcher);
        await externalInterfaceServer.StartAsync();
        Logger.Info("External Interface Server started, Bind IP: " +
                    $"{config.ExternalInterfaceIp}, Port: {config.ExternalInterfacePort}");
        externalInterfaceServer.OnInfo = Logger.Info;
        externalInterfaceServer.OnWarning = Logger.Warning;
        externalInterfaceServer.OnError = Logger.Error;
        return externalInterfaceServer;
    }
}