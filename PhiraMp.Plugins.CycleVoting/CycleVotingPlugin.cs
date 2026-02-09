using System.ComponentModel.Composition;
using PhiraMp.Server;
using PhiraMp.Server.Plugins;
using PhiraMp.Server.Models;
using PhiraMp.Core;

namespace PhiraMp.Plugins.CycleVoting;

/// <summary>
/// Cycle voting plugin - fully manages all voting logic independently from server
/// Stores all state in plugin, not in Room
/// </summary>
[Export(typeof(IPluginModule))]
[Export(typeof(ISelectChartHandler))]
[Export(typeof(IRequestStartHandler))]
[Export(typeof(ICycleModeChangeHandler))]
[Export(typeof(IRoomStateHandler))]
public class CycleVotingPlugin : IPluginModule, ISelectChartHandler, IRequestStartHandler, ICycleModeChangeHandler, IRoomStateHandler
{
    private readonly Dictionary<string, RoomVotingState> _roomVotingStates = new();
    private PluginContext? _context;

    public async Task InitializeAsync(PluginContext context)
    {
        _context = context;
        Console.WriteLine("[CycleVoting] Initialized - Full cycle voting support with no server dependencies");
        await Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        Console.WriteLine("[CycleVoting] Shutting down");
        _roomVotingStates.Clear();
        return Task.CompletedTask;
    }

    private RoomVotingState GetOrCreateVotingState(string roomId)
    {
        if (!_roomVotingStates.ContainsKey(roomId))
        {
            _roomVotingStates[roomId] = new RoomVotingState();
        }
        return _roomVotingStates[roomId];
    }

    public async Task HandleSelectChartAsync(SelectChartContext context)
    {
        var room = context.Room;
        var user = context.User;
        var chart = context.Chart;

        // Only handle voting if room is in cycle mode
        if (!room.Cycle)
            return;

        var state = GetOrCreateVotingState(room.Id.Value);
        
        // If voting is enabled for this room, record the vote
        if (state.VotingEnabled)
        {
            state.RecordVote(user.Id, chart);
            Console.WriteLine($"[CycleVoting] User {user.Name} voted for chart {chart.Name} in room {room.Id}");
        }

        await Task.CompletedTask;
    }

    public async Task HandleRequestStartAsync(RequestStartContext context)
    {
        var room = context.Room;
        
        // Only handle if room is in cycle mode
        if (!room.Cycle)
            return;

        var state = GetOrCreateVotingState(room.Id.Value);
        
        // If voting is enabled, select a random chart from votes
        if (state.VotingEnabled)
        {
            var selectedChart = state.SelectRandomChart();
            if (selectedChart == null)
                throw new Exception("No chart votes recorded");

            // Set the selected chart on the room
            room.Chart = selectedChart;
            Console.WriteLine($"[CycleVoting] Randomly selected chart {selectedChart.Name} from {state.VoteCount} votes");

            // Revoke fake host status from all non-host users
            var users = room.GetUsers();
            foreach (var user in users)
            {
                if (!room.IsHost(user))
                {
                    await user.TrySendAsync(new ChangeHostCommand(false));
                }
            }

            // Clear votes for next round
            state.ClearVotes();

            // Notify all users of the final selected chart
            await room.OnStateChangeAsync();
        }
    }

    public async Task HandleCycleModeChangeAsync(CycleModeChangeContext context)
    {
        var room = context.Room;
        var cycleEnabled = context.CycleEnabled;
        
        var state = GetOrCreateVotingState(room.Id.Value);
        
        // Enable voting when cycle mode is enabled
        state.VotingEnabled = cycleEnabled;
        
        if (cycleEnabled)
        {
            // Grant fake host status to all non-host users so they can select charts
            var users = room.GetUsers();
            foreach (var user in users)
            {
                if (!room.IsHost(user))
                {
                    await user.TrySendAsync(new ChangeHostCommand(true));
                }
            }
            
            Console.WriteLine($"[CycleVoting] Cycle voting enabled for room {room.Id}");
        }
        else
        {
            // Revoke fake host status when cycle mode is disabled
            var users = room.GetUsers();
            foreach (var user in users)
            {
                if (!room.IsHost(user))
                {
                    await user.TrySendAsync(new ChangeHostCommand(false));
                }
            }
            
            state.ClearVotes();
            Console.WriteLine($"[CycleVoting] Cycle voting disabled for room {room.Id}");
        }
    }

    public async Task HandleStateChangeAsync(RoomStateContext context)
    {
        var room = context.Room;
        
        // Only handle if room is in cycle mode with voting
        if (!room.Cycle)
            return;

        var state = GetOrCreateVotingState(room.Id.Value);
        if (!state.VotingEnabled)
            return;

        // When returning to SelectChart state after game, grant host status again
        if (context.NewState == "SelectChart")
        {
            var users = room.GetUsers();
            foreach (var user in users)
            {
                if (!room.IsHost(user))
                {
                    await user.TrySendAsync(new ChangeHostCommand(true));
                }
            }
            
            await room.SendAsync(new ChatMessage(-1, "[CycleVoting] New round - all players can select charts!"));
        }
    }

    private class RoomVotingState
    {
        public bool VotingEnabled { get; set; }
        private readonly Dictionary<int, ChartInfo> _votes = new();

        public void RecordVote(int userId, ChartInfo chart)
        {
            _votes[userId] = chart;
        }

        public ChartInfo? SelectRandomChart()
        {
            if (_votes.Count == 0)
                return null;

            var charts = _votes.Values.ToList();
            return charts[Random.Shared.Next(charts.Count)];
        }

        public void ClearVotes()
        {
            _votes.Clear();
        }

        public int VoteCount => _votes.Count;
    }
}
