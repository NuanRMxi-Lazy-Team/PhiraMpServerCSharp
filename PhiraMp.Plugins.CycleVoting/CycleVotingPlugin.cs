using PhiraMp.Plugin.SDK;

namespace PhiraMp.Plugins.CycleVoting;

[Plugin("CycleVotingPlugin", "1.0.0", "PhiraMP Team", "Enhances cycle voting mode with statistics and announcements")]
public class CycleVotingPlugin : PluginBase
{
    private readonly Dictionary<string, RoomVoteStats> _roomStats = new();

    public override string Name => "CycleVotingPlugin";
    public override string Version => "1.0.0";

    public override async Task OnLoadAsync(IPluginContext context)
    {
        await base.OnLoadAsync(context);

        // Subscribe to room state changes
        context.ServerAPI.SubscribeToRoomStateChange(OnRoomStateChange);
        
        // Subscribe to room messages for vote announcements
        context.ServerAPI.SubscribeToRoomMessages(OnRoomMessage);

        Context.Logger.Info("CycleVotingPlugin loaded successfully");
    }

    public override Task OnUnloadAsync()
    {
        Context.Logger.Info("CycleVotingPlugin unloading");
        _roomStats.Clear();
        return Task.CompletedTask;
    }

    private async Task OnRoomStateChange(IRoomContext room, string newState)
    {
        if (!room.IsCycleVotingMode)
            return;

        // Initialize stats for room if not exists
        if (!_roomStats.ContainsKey(room.RoomId))
        {
            _roomStats[room.RoomId] = new RoomVoteStats();
        }

        var stats = _roomStats[room.RoomId];

        switch (newState)
        {
            case "SelectChart":
                // Announce voting mode when entering chart selection
                if (room.IsCycleMode)
                {
                    await room.SendMessageAsync("[CycleVoting] Cycle voting mode is active. All players can select charts!");
                    stats.VotingRoundStarted();
                }
                break;

            case "WaitingForReady":
                // Announce chart selection complete
                stats.VotingRoundEnded();
                await room.SendMessageAsync($"[CycleVoting] Chart selected! Total voting rounds: {stats.TotalRounds}");
                break;

            case "Playing":
                // Game started
                Context.Logger.Debug($"Room {room.RoomId} started playing in cycle voting mode");
                break;
        }
    }

    private async Task OnRoomMessage(IRoomContext room, IUserContext user, string message)
    {
        // Check if user is selecting a chart in cycle voting mode
        if (!room.IsCycleVotingMode || !room.IsCycleMode)
            return;

        // This is a simple example - in real implementation we'd track chart selections
        // For now, just provide helpful messages when users ask about voting
        if (message.ToLower().Contains("vote") || message.ToLower().Contains("how"))
        {
            await room.SendMessageToUserAsync(user, 
                "[CycleVoting] In cycle voting mode, all players can select charts. " +
                "The host will randomly choose from all votes when starting the game.");
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
