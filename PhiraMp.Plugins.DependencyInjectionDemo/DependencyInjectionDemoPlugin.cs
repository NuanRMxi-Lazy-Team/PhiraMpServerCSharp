using System.ComponentModel.Composition;
using PhiraMp.Server.Models;
using PhiraMp.Server.Plugins;

namespace PhiraMp.Plugins.DependencyInjectionDemo;

/// <summary>
/// 玩家统计服务接口 - 由提供者插件注册，消费者插件使用
/// 这是插件间通信的契约
/// </summary>
public interface IPlayerStatsService
{
    /// <summary>
    /// 记录玩家进入房间
    /// </summary>
    void RecordRoomJoin(string username, string roomId);
    
    /// <summary>
    /// 获取玩家统计信息
    /// </summary>
    PlayerStats GetPlayerStats(string username);
    
    /// <summary>
    /// 获取总统计信息
    /// </summary>
    string GetTotalStats();
}

/// <summary>
/// 玩家统计数据
/// </summary>
public class PlayerStats
{
    public string Username { get; set; } = "";
    public int RoomJoinCount { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
}

/// <summary>
/// 服务提供者插件 - 注册玩家统计服务
/// 此插件实现并注册服务，供其他插件使用
/// </summary>
[Export(typeof(IPluginModule))]
[Export(typeof(IUserJoinHandler))]
public class PlayerStatsProviderPlugin : IPluginModule, IUserJoinHandler
{
    private IPluginLogger _logger = null!;
    private IPluginServiceProvider _serviceProvider = null!;
    private readonly PlayerStatsService _statsService = new();

    public async Task InitializeAsync(PluginContext context)
    {
        _logger = context.Logger;
        _serviceProvider = context.ServiceProvider;

        // 注册服务供其他插件使用
        _serviceProvider.RegisterService<IPlayerStatsService>(_statsService);
        
        _logger.Info("玩家统计服务提供者插件已初始化");
        _logger.Info("- 已注册服务: IPlayerStatsService");
        _logger.Info("- 其他插件现在可以通过 ServiceProvider.GetService<IPlayerStatsService>() 获取此服务");
        
        await Task.CompletedTask;
    }

    public async Task HandleUserJoinAsync(UserEventContext context)
    {
        // 记录玩家加入房间
        _statsService.RecordRoomJoin(context.User.Name, context.Room.Id.Value);
        
        _logger.Debug($"记录玩家 {context.User.Name} 加入房间 {context.Room.Id.Value}");
        
        await Task.CompletedTask;
    }

    public async Task ShutdownAsync()
    {
        _logger.Info("玩家统计服务提供者插件已关闭");
        _logger.Info(_statsService.GetTotalStats());
        await Task.CompletedTask;
    }
}

/// <summary>
/// 玩家统计服务实现
/// </summary>
internal class PlayerStatsService : IPlayerStatsService
{
    private readonly Dictionary<string, PlayerStats> _stats = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public void RecordRoomJoin(string username, string roomId)
    {
        lock (_lock)
        {
            if (!_stats.TryGetValue(username, out var stats))
            {
                stats = new PlayerStats
                {
                    Username = username,
                    FirstSeen = DateTime.UtcNow
                };
                _stats[username] = stats;
            }

            stats.RoomJoinCount++;
            stats.LastSeen = DateTime.UtcNow;
        }
    }

    public PlayerStats GetPlayerStats(string username)
    {
        lock (_lock)
        {
            return _stats.TryGetValue(username, out var stats) 
                ? stats 
                : new PlayerStats { Username = username };
        }
    }

    public string GetTotalStats()
    {
        lock (_lock)
        {
            var totalPlayers = _stats.Count;
            var totalJoins = _stats.Values.Sum(s => s.RoomJoinCount);
            return $"总玩家数: {totalPlayers}, 总加入次数: {totalJoins}";
        }
    }
}

/// <summary>
/// 服务消费者插件 - 使用玩家统计服务
/// 此插件依赖于 PlayerStatsProviderPlugin 提供的服务
/// 演示了插件间的依赖注入
/// </summary>
[Export(typeof(IPluginModule))]
[Export(typeof(IRoomMessageHandler))]
public class PlayerStatsConsumerPlugin : IPluginModule, IRoomMessageHandler
{
    private IPluginLogger _logger = null!;
    private IPluginAPI _api = null!;
    private IPlayerStatsService? _statsService;

    public async Task InitializeAsync(PluginContext context)
    {
        _logger = context.Logger;
        _api = context.API;

        // 从服务提供者获取统计服务
        _statsService = context.ServiceProvider.GetService<IPlayerStatsService>();

        if (_statsService != null)
        {
            _logger.Info("玩家统计消费者插件已初始化");
            _logger.Info("- 成功获取 IPlayerStatsService 服务");
            _logger.Info("- 命令: /stats [玩家名] - 查看玩家统计");
            _logger.Info("- 命令: /stats_total - 查看总体统计");
        }
        else
        {
            _logger.Warning("玩家统计消费者插件已初始化");
            _logger.Warning("- 警告: 未找到 IPlayerStatsService 服务");
            _logger.Warning("- 请确保 PlayerStatsProviderPlugin 已加载");
        }
        
        await Task.CompletedTask;
    }

    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        if (_statsService == null)
        {
            return; // 服务不可用
        }

        var message = context.Message.Trim();
        
        // 查看玩家统计
        if (message.StartsWith("/stats ", StringComparison.OrdinalIgnoreCase))
        {
            var username = message.Substring(7).Trim();
            if (string.IsNullOrEmpty(username))
            {
                await _api.SendRoomMessageAsync(context.Room, "用法: /stats [玩家名]");
                return;
            }

            var stats = _statsService.GetPlayerStats(username);
            
            if (stats.RoomJoinCount == 0)
            {
                await _api.SendRoomMessageAsync(context.Room, $"未找到玩家 {username} 的统计信息");
            }
            else
            {
                var message_text = $"玩家 {stats.Username} 的统计:\n" +
                          $"- 加入房间次数: {stats.RoomJoinCount}\n" +
                          $"- 首次出现: {stats.FirstSeen:yyyy-MM-dd HH:mm:ss}\n" +
                          $"- 最后出现: {stats.LastSeen:yyyy-MM-dd HH:mm:ss}";
                await _api.SendRoomMessageAsync(context.Room, message_text);
            }
            
            _logger.Info($"用户 {context.User.Name} 查询了 {username} 的统计");
            return;
        }
        
        // 查看总体统计
        if (message.Equals("/stats_total", StringComparison.OrdinalIgnoreCase))
        {
            var totalStats = _statsService.GetTotalStats();
            await _api.SendRoomMessageAsync(context.Room, $"[统计] {totalStats}");
            
            _logger.Info($"用户 {context.User.Name} 查询了总体统计");
            return;
        }
        
        await Task.CompletedTask;
    }

    public async Task ShutdownAsync()
    {
        _logger.Info("玩家统计消费者插件已关闭");
        await Task.CompletedTask;
    }
}
