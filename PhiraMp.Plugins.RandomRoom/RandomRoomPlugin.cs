using System.ComponentModel.Composition;
using PhiraMp.Server.Models;
using PhiraMp.Server.Plugins;

namespace PhiraMp.Plugins.RandomRoom;

/// <summary>
/// 随机房间插件 - 当用户尝试加入 "random" 房间时，将其重定向到一个随机房间
/// 同时防止创建名为 "random" 的房间
/// </summary>
[Export(typeof(IPluginModule))]
[Export(typeof(IJoinRoomRequestHandler))]
[Export(typeof(ICreateRoomRequestHandler))]
public class RandomRoomPlugin : IPluginModule, IJoinRoomRequestHandler, ICreateRoomRequestHandler
{
    private IPluginLogger _logger = null!;
    private IPluginAPI _api = null!;
    private IPluginConfig _config = null!;

    // 保留的房间名列表（不能创建这些房间）
    private HashSet<string> _reservedRoomNames = [];
    private bool _redirectMessageEnabled = true;
    private string _redirectMessage = "正在将您重定向到随机房间...";

    /// <summary>
    /// 插件初始化
    /// </summary>
    public async Task InitializeAsync(PluginContext context)
    {
        _logger = context.Logger;
        _api = context.API;
        _config = context.Config;

        // 加载配置
        await LoadConfigAsync();

        _logger.Info("随机房间插件已初始化");
        _logger.Info($"- 保留房间名: {string.Join(", ", _reservedRoomNames)}");
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    private async Task LoadConfigAsync()
    {
        // 定义配置类
        var defaultConfig = new RandomRoomConfig
        {
            ReservedRoomNames = ["random", "rand", "随机"],
            RedirectMessageEnabled = true,
            RedirectMessage = "正在将您重定向到随机房间..."
        };

        // 加载或创建配置
        var config = _config.Load("random_room.yml", defaultConfig);

        // 设置变量
        _reservedRoomNames = new HashSet<string>(
            config.ReservedRoomNames.Select(n => n.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);
        _redirectMessageEnabled = config.RedirectMessageEnabled;
        _redirectMessage = config.RedirectMessage;

        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理加入房间请求
    /// </summary>
    public async Task HandleJoinRoomRequestAsync(JoinRoomRequestContext context)
    {
        var requestedRoomName = context.OriginalRoomId.Value;

        // 检查是否是保留房间名
        if (_reservedRoomNames.Contains(requestedRoomName))
        {
            _logger.Info($"用户 {context.User.Name} 请求加入保留房间 '{requestedRoomName}'，正在重定向到随机房间");

            // 获取所有可用的房间
            var allRooms = _api.GetAllRooms();
            // 筛选条件：
            // 1. 房间未锁定
            // 2. 房间处于选歌状态（SelectChart），这样用户可以加入
            // 3. 房间名不是保留名称
            var availableRooms = allRooms
                .Where(r => r is { Locked: false, State: InternalRoomState.SelectChart } &&
                            !_reservedRoomNames.Contains(r.Id.Value))
                .ToList();

            if (availableRooms.Count == 0)
            {
                _logger.Warning("没有可用的房间可供重定向");
                throw new Exception("没有可用的房间，请手动创建或加入其他房间。");
            }

            // 随机选择一个房间
            var random = new Random();
            var randomRoom = availableRooms[random.Next(availableRooms.Count)];

            _logger.Info($"重定向到房间 '{randomRoom.Id.Value}' (当前 {randomRoom.GetAllUsers().Count} 人)");
            await _api.SendPrivateMessageAsync(context.User, $"您已被重定向到房间 {randomRoom.Id.Value}");

            // 修改目标房间 ID
            context.TargetRoomId = randomRoom.Id;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 处理创建房间请求
    /// </summary>
    public async Task HandleCreateRoomRequestAsync(CreateRoomRequestContext context)
    {
        var requestedRoomName = context.RoomId.Value;

        // 检查是否是保留房间名
        if (_reservedRoomNames.Contains(requestedRoomName))
        {
            _logger.Warning($"用户 {context.User.Name} 尝试创建保留房间 '{requestedRoomName}'，已阻止");
            throw new Exception($"房间名 '{requestedRoomName}' 是保留名称，不能创建。");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 插件关闭
    /// </summary>
    public async Task ShutdownAsync()
    {
        _logger.Info("随机房间插件已关闭");
        await Task.CompletedTask;
    }
}