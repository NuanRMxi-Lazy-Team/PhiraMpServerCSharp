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
    private List<string> _motds = [];
    
    public async Task InitializeAsync(PluginContext context)
    {
        _context = context;
        _logger = context.Logger;
        _api = context.API;
        _config = context.Config;
        // 定义配置类
        var defaultConfig = new PluginConfig()
        {
            Motds = ["欢迎回来！", "您吃了吗？", "饿了的话吃完再来玩吧！"],
        };
        // 加载或创建配置
        var config = await _config.LoadAsync("motd.yml", defaultConfig);
        _motds = config.Motds;
        _logger.Info($"Motds loaded: {string.Join(", ", _motds)}");
    }

    public async Task ShutdownAsync()
    {
        await Task.CompletedTask;
    }

    public async Task HandleUserConnectAsync(UserConnectContext context)
    {
        if (!context.IsReconnect)
        {
            foreach (var motd in _motds)
            {
                await _api.SendPrivateMessageAsync(context.User, motd);
            }
        }
    }
}