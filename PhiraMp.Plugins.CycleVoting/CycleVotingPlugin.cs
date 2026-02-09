using System.ComponentModel.Composition;
using PhiraMp.Server;
using PhiraMp.Server.Plugins;
using PhiraMp.Server.Models;
using PhiraMp.Core;

namespace PhiraMp.Plugins.CycleVoting;

/// <summary>
/// 循环投票插件 - 完全独立管理所有投票逻辑
/// 所有状态都存储在插件内部，不依赖服务器核心
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
        context.Logger.Info("已初始化 - 提供完整的循环投票支持，无服务器依赖");
        await Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _context?.Logger.Info("正在关闭");
        _roomVotingStates.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取或创建房间的投票状态
    /// </summary>
    private RoomVotingState GetOrCreateVotingState(string roomId)
    {
        if (!_roomVotingStates.ContainsKey(roomId))
        {
            _roomVotingStates[roomId] = new RoomVotingState();
        }
        return _roomVotingStates[roomId];
    }

    /// <summary>
    /// 处理选歌事件
    /// </summary>
    public async Task HandleSelectChartAsync(SelectChartContext context)
    {
        var room = context.Room;
        var user = context.User;
        var chart = context.Chart;

        // 只在循环模式下处理投票
        if (!room.Cycle)
            return;

        var state = GetOrCreateVotingState(room.Id.Value);
        
        // 如果投票已启用，记录投票
        if (state.VotingEnabled)
        {
            state.RecordVote(user.Id, chart);
            _context?.Logger.Debug($"用户 {user.Name} 在房间 {room.Id} 投票给谱面 {chart.Name}");
            
            // 通知房间
            await _context!.API.SendRoomMessageAsync(
                room, 
                $"📊 {user.Name} 投票: {chart.Name} (共 {state.VoteCount} 票)");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理游戏开始请求
    /// </summary>
    public async Task HandleRequestStartAsync(RequestStartContext context)
    {
        var room = context.Room;
        
        // 只在循环模式下处理
        if (!room.Cycle)
            return;

        var state = GetOrCreateVotingState(room.Id.Value);
        
        // 如果投票已启用，从投票中随机选择谱面
        if (state.VotingEnabled)
        {
            var selectedChart = state.SelectRandomChart();
            if (selectedChart == null)
                throw new Exception("没有收到任何投票");

            // 设置选中的谱面
            room.Chart = selectedChart;
            _context?.Logger.Info($"房间 {room.Id} 从 {state.VoteCount} 票中随机选择了谱面 {selectedChart.Name}");

            // 撤销所有非房主用户的假房主权限
            var users = room.GetUsers();
            foreach (var user in users)
            {
                if (!room.IsHost(user))
                {
                    await user.TrySendAsync(new ChangeHostCommand(false));
                }
            }

            // 清除投票，为下一轮做准备
            state.ClearVotes();

            // 通知所有用户最终选择的谱面
            await room.OnStateChangeAsync();
            
            // 发送通知消息
            await _context!.API.SendRoomMessageAsync(
                room, 
                $"🎲 投票结果: {selectedChart.Name}");
        }
    }

    /// <summary>
    /// 处理循环模式变化
    /// </summary>
    public async Task HandleCycleModeChangeAsync(CycleModeChangeContext context)
    {
        var room = context.Room;
        var cycleEnabled = context.CycleEnabled;
        
        var state = GetOrCreateVotingState(room.Id.Value);
        
        // 启用或禁用投票
        state.VotingEnabled = cycleEnabled;
        
        if (cycleEnabled)
        {
            // 授予所有非房主用户假房主权限，以便他们可以选歌
            var users = room.GetUsers();
            foreach (var user in users)
            {
                if (!room.IsHost(user))
                {
                    await user.TrySendAsync(new ChangeHostCommand(true));
                }
            }
            
            _context?.Logger.Info($"房间 {room.Id} 已启用循环投票");
            await _context!.API.SendRoomMessageAsync(room, "🗳️ 循环投票模式已启用 - 所有玩家都可以选歌投票！");
        }
        else
        {
            // 撤销假房主权限
            var users = room.GetUsers();
            foreach (var user in users)
            {
                if (!room.IsHost(user))
                {
                    await user.TrySendAsync(new ChangeHostCommand(false));
                }
            }
            
            state.ClearVotes();
            _context?.Logger.Info($"房间 {room.Id} 已禁用循环投票");
            await _context!.API.SendRoomMessageAsync(room, "❌ 循环投票模式已禁用");
        }
    }

    /// <summary>
    /// 处理房间状态变化
    /// </summary>
    public async Task HandleStateChangeAsync(RoomStateContext context)
    {
        var room = context.Room;
        
        // 只在循环模式且投票启用时处理
        if (!room.Cycle)
            return;

        var state = GetOrCreateVotingState(room.Id.Value);
        if (!state.VotingEnabled)
            return;

        // 当回到选歌状态时，重新授予房主权限
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
            
            await _context!.API.SendRoomMessageAsync(
                room, 
                "🔄 新回合开始 - 所有玩家可以选歌投票！");
        }
    }

    /// <summary>
    /// 房间投票状态（内部类）
    /// </summary>
    private class RoomVotingState
    {
        /// <summary>是否启用投票</summary>
        public bool VotingEnabled { get; set; }
        
        /// <summary>用户投票记录</summary>
        private readonly Dictionary<int, ChartInfo> _votes = new();

        /// <summary>记录用户投票</summary>
        public void RecordVote(int userId, ChartInfo chart)
        {
            _votes[userId] = chart;
        }

        /// <summary>从投票中随机选择一个谱面</summary>
        public ChartInfo? SelectRandomChart()
        {
            if (_votes.Count == 0)
                return null;

            var charts = _votes.Values.ToList();
            return charts[Random.Shared.Next(charts.Count)];
        }

        /// <summary>清除所有投票</summary>
        public void ClearVotes()
        {
            _votes.Clear();
        }

        /// <summary>获取投票数量</summary>
        public int VoteCount => _votes.Count;
    }
}
