using System.ComponentModel.Composition;
using PhiraMp.Server;
using PhiraMp.Server.Plugins;
using PhiraMp.Server.Models;

namespace PhiraMp.Plugins.CycleVoting;

/// <summary>
/// Cycle voting plugin using MEF - demonstrates flexible plugin architecture
/// Can export multiple contracts and access server internals directly
/// </summary>
[Export(typeof(IPluginModule))]
[Export(typeof(IRoomStateHandler))]
[Export(typeof(IRoomMessageHandler))]
public class CycleVotingPlugin : IPluginModule, IRoomStateHandler, IRoomMessageHandler
{
    private readonly Dictionary<string, RoomVoteStats> _roomStats = new();
    private PluginContext? _context;

    public async Task InitializeAsync(PluginContext context)
    {
        _context = context;
        Console.WriteLine("[CycleVotingPlugin] Initialized with MEF - Maximum flexibility!");
        Console.WriteLine($"[CycleVotingPlugin] Can access server state directly: {context.ServerState.Rooms.Count} rooms");
        await Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        Console.WriteLine("[CycleVotingPlugin] Shutting down");
        _roomStats.Clear();
        return Task.CompletedTask;
    }

    public async Task HandleStateChangeAsync(RoomStateContext context)
    {
        if (!context.Room.CycleVotingMode)
            return;

        // Initialize stats for room if not exists
        if (!_roomStats.ContainsKey(context.Room.Id.Value))
        {
            _roomStats[context.Room.Id.Value] = new RoomVoteStats();
        }

        var stats = _roomStats[context.Room.Id.Value];

        switch (context.NewState)
        {
            case "SelectChart":
                // Announce voting mode when entering chart selection
                if (context.Room.Cycle)
                {
                    await context.Room.SendAsync(new Core.ChatMessage(-1, 
                        "[CycleVoting] Cycle voting mode is active. All players can select charts!"));
                    stats.VotingRoundStarted();
                }
                break;

            case "WaitingForReady":
                // Announce chart selection complete
                stats.VotingRoundEnded();
                await context.Room.SendAsync(new Core.ChatMessage(-1, 
                    $"[CycleVoting] Chart selected! Total voting rounds: {stats.TotalRounds}"));
                break;

            case "Playing":
                // Game started
                Console.WriteLine($"[CycleVotingPlugin] Room {context.Room.Id} started playing in cycle voting mode");
                break;
        }
    }

    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        // Check if user is asking about voting
        if (!context.Room.CycleVotingMode || !context.Room.Cycle)
            return;

        var msg = context.Message.ToLower();
        if (msg.Contains("vote") || msg.Contains("how") || msg == "?")
        {
            var help = "[CycleVoting] In cycle voting mode, all players can select charts. " +
                      "The host will randomly choose from all votes when starting the game.\n" +
                      "Try /info for more details.";
            await context.Room.SendAsync(new Core.ChatMessage(-1, help));
        }
    }

    private class RoomVoteStats
    {
        public int TotalRounds { get; private set; }
        public DateTime? CurrentRoundStartTime { get; private set; }

        public void VotingRoundStarted()
        {
            CurrentRoundStartTime = DateTime.UtcNow;
        }

        public void VotingRoundEnded()
        {
            if (CurrentRoundStartTime.HasValue)
            {
                TotalRounds++;
                CurrentRoundStartTime = null;
            }
        }
    }
}
