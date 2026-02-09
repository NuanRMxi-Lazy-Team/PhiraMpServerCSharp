using System.ComponentModel.Composition;
using PhiraMp.Server;
using PhiraMp.Server.Plugins;

namespace PhiraMp.Plugins.SinglePlayerPrevention;

/// <summary>
/// Plugin to prevent single-player games - requires at least 2 players to start
/// </summary>
[Export(typeof(IPluginModule))]
[Export(typeof(IRequestStartHandler))]
public class SinglePlayerPreventionPlugin : IPluginModule, IRequestStartHandler
{
    private int _minPlayers = 2;

    public async Task InitializeAsync(PluginContext context)
    {
        Console.WriteLine("[SinglePlayerPrevention] Initialized - Games require at least 2 players");
        await Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        Console.WriteLine("[SinglePlayerPrevention] Shutting down");
        return Task.CompletedTask;
    }

    public async Task HandleRequestStartAsync(RequestStartContext context)
    {
        var playerCount = context.Room.GetAllUsers().Count;
        
        if (playerCount < _minPlayers)
        {
            throw new Exception("If no one is looking for you to play, you can go out and relax.");
        }
        
        await Task.CompletedTask;
    }
}
