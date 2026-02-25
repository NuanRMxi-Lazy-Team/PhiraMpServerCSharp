using System.ComponentModel.Composition;
using PhiraMp.Core;
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
    private PluginContext _context = null!;
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
        _context = context;
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
        var config = await _config.LoadAsync("random_room.yml", defaultConfig);

        // 设置变量
        _reservedRoomNames = new HashSet<string>(
            config.ReservedRoomNames.Select(n => n.ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);
        _redirectMessageEnabled = config.RedirectMessageEnabled;
        _redirectMessage = config.RedirectMessage;
    }

    /// <summary>
    /// 处理加入房间请求
    /// </summary>
    public async Task HandleJoinRoomRequestAsync(JoinRoomRequestContext context)
    {
        var requestedRoomName = context.RoomId.Value;

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
            // 4. 房间未满
            var availableRooms = allRooms.Where(r =>
                !r.Locked &&
                r.GetClientRoomState().State == RoomState.SelectChart &&
                !_reservedRoomNames.Contains(r.Id.Value) &&
                r.GetAllUsers().Count < _context.ServerState.Config.RoomMaxPlayers
            ).ToList();

            if (availableRooms.Count == 0)
            {
                _logger.Warning("没有可用的房间可供重定向");
                throw new Exception("没有可用的房间，请手动创建或加入其他房间。");
            }

            // 随机选择一个房间
            var random = new Random();
            var randomRoom = availableRooms[random.Next(availableRooms.Count)];

            _logger.Info($"将重定向到房间 '{randomRoom.Id.Value}' (当前 {randomRoom.GetAllUsers().Count} 人)");
            if (context.Monitor && !context.User.CanMonitor())
                throw new Exception("不能监听房间，因为您没有权限。");
            if (!randomRoom.AddUser(context.User, context.Monitor))
                throw new Exception("不巧，这个房间可能刚刚好满了，你可以再试一次。");
            
            context.User.IsMonitor = context.Monitor;
            if (context.Monitor && !randomRoom.Live)
            {
                randomRoom.Live = true;
                _logger.Info($"Room {context.RoomId.Value} goes live");
            }
            
            await randomRoom.BroadcastAsync(new OnJoinRoomCommand(context.User.ToInfo()));
            await randomRoom.SendAsync(new JoinRoomMessage(context.User.Id, context.User.Name));
            context.User.Room = randomRoom;
            
            await _api.SendPrivateMessageAsync(context.User, $"您被重定向到了房间 '{randomRoom.Id.Value}'");
            
            await _api.SendCommandAsync(context.User,
                new JoinRoomResponseCommand(new JoinRoomResponse(
                    randomRoom.GetClientRoomState(),
                    randomRoom.GetAllUsers().Select(u => u.ToInfo()).ToList(),
                    randomRoom.Live)));
            
            context.IsHandled = true;
        }
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