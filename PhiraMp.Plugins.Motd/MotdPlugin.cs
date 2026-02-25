using System.ComponentModel.Composition;
using PhiraMp.Server.Plugins;

namespace PhiraMp.Plugins.Motd;

[Export(typeof(IPluginModule))]
[Export(typeof(IUserConnectHandler))]
public class MotdPlugin : IPluginModule, IUserConnectHandler
{
    private IPluginLogger _logger = null!;
    private IPluginAPI _api = null!;
    private PluginContext _context = null!;
    private IPluginConfig _config = null!;
    private PluginConfig _pluginConfig = null!;
    
    public async Task InitializeAsync(PluginContext context)
    {
        _context = context;
        _logger = context.Logger;
        _api = context.API;
        _config = context.Config;
        // 定义配置类
        var defaultConfig = new PluginConfig()
        {
            Motds = ["欢迎回到{serverName}！", "{userName}吃了吗？", "饿了的话吃完再来玩吧！还有{playerCount}个小伙伴等着你呢！"],
            ServerName = "undefined",
        };
        // 加载或创建配置
        _pluginConfig = await _config.LoadAsync("motd.yml", defaultConfig);
        _logger.Info($"Motds loaded: {string.Join(", ", _pluginConfig.Motds)}");
    }

    public async Task ShutdownAsync()
    {
        await Task.CompletedTask;
    }

    public async Task HandleUserConnectAsync(UserConnectContext context)
    {
        if (!context.IsReconnect)
        {
            foreach (var motd in _pluginConfig.Motds)
            {
                // userName
                var personalizedMotd = motd.Replace("{userName}", context.User.Name);
                // serverName
                personalizedMotd = personalizedMotd.Replace("{serverName}", _pluginConfig.ServerName);
                // playerCount
                personalizedMotd  = personalizedMotd.Replace("{playerCount}", _api.GetAllUsers().Count().ToString());
                await _api.SendPrivateMessageAsync(context.User, personalizedMotd);
            }
        }
    }
}