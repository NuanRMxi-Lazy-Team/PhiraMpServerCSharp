using System.ComponentModel.Composition;
using PhiraMp.Server.Plugins;

namespace PhiraMp.Plugins.SinglePlayerPrevention;

/// <summary>
/// 单人游戏防止插件 - 要求至少 2 名玩家才能开始游戏
/// 通过配置文件可以自定义最少玩家数
/// </summary>
[Export(typeof(IPluginModule))]
[Export(typeof(IRequestStartHandler))]
public class SinglePlayerPreventionPlugin : IPluginModule, IRequestStartHandler
{
    private PluginContext? _context;
    private PluginConfig? _config;

    /// <summary>
    /// 插件配置类
    /// </summary>
    private class PluginConfig
    {
        /// <summary>最少玩家数（默认 2）</summary>
        public int MinPlayers { get; set; } = 2;
        
        /// <summary>错误提示消息</summary>
        public string ErrorMessage { get; set; } = "孤单一人？不如出去走走~";
    }

    public async Task InitializeAsync(PluginContext context)
    {
        _context = context;
        
        // 加载配置
        _config = context.Config.Load("single_player_prevention.yml", new PluginConfig());
        
        context.Logger.Info($"已初始化 - 要求至少 {_config.MinPlayers} 名玩家才能开始游戏");
        await Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _context?.Logger.Info("正在关闭");
        return Task.CompletedTask;
    }

    public async Task HandleRequestStartAsync(RequestStartContext context)
    {
        var playerCount = context.Room.GetAllUsers().Count;
        
        if (playerCount < _config!.MinPlayers)
        {
            var message = _config.ErrorMessage.Replace("{count}", playerCount.ToString())
                                             .Replace("{min}", _config.MinPlayers.ToString());
            
            _context?.Logger.Debug($"阻止房间 {context.Room.Id} 开始游戏：只有 {playerCount} 名玩家（需要 {_config.MinPlayers} 名）");
            
            // 向房间发送提示消息
            await _context!.API.SendRoomMessageAsync(
                context.Room, 
                $"⚠至少需要 {_config.MinPlayers} 名玩家才能开始游戏（当前 {playerCount} 名）");
            
            throw new Exception(message);
        }
        
        await Task.CompletedTask;
    }
}
