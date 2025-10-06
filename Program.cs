using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhiraMpServer.Server;

namespace PhiraMpServer;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Phira Multiplayer Server (C# Implementation)");

        rootCommand.SetHandler(async () =>
        {
            await RunServerAsync();
        });

        return await rootCommand.InvokeAsync(args);
    }

    static async Task RunServerAsync()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddConsole()
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning);
        });

        var logger = loggerFactory.CreateLogger<Program>();

        var config = PhiraMpServer.Server.ServerConfig.Load();

        logger.LogInformation("Starting Phira Multiplayer Server");
        logger.LogInformation("Bind IP: {BindIp}, Port: {Port}, Room Max Players: {MaxPlayers}", 
            config.BindIp, config.Port, config.RoomMaxPlayers);
        logger.LogInformation("Press Ctrl+C to stop the server");

        using var server = new Server.PhiraMpServer(loggerFactory, config);

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            logger.LogInformation("Shutting down...");
            e.Cancel = true;
            cts.Cancel();
        };

        var serverTask = server.StartAsync();

        await Task.WhenAny(serverTask, Task.Delay(Timeout.Infinite, cts.Token));

        logger.LogInformation("Server stopped");
    }
}
