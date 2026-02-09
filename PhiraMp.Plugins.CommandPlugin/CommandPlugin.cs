using System.ComponentModel.Composition;
using PhiraMp.Server;
using PhiraMp.Server.Plugins;

namespace PhiraMp.Plugins.CommandPlugin;

/// <summary>
/// 命令插件 - 处理房间内的命令
/// 使用 MEF 发现，支持 /kick, /help, /info 等命令
/// </summary>
[Export(typeof(IPluginModule))]
[Export(typeof(IRoomMessageHandler))]
public class CommandPlugin : IPluginModule, IRoomMessageHandler
{
    private readonly Dictionary<string, CommandHandler> _commands = new();
    private PluginContext? _context;

    /// <summary>
    /// 命令处理器委托
    /// </summary>
    private delegate Task CommandHandler(Room room, User user, string[] args);

    public async Task InitializeAsync(PluginContext context)
    {
        _context = context;
        
        // 注册命令
        RegisterCommand("kick", HandleKickCommand, "踢出用户（仅房主）");
        RegisterCommand("help", HandleHelpCommand, "显示帮助信息");
        RegisterCommand("info", HandleInfoCommand, "显示插件信息");
        RegisterCommand("list", HandleListCommand, "列出房间内所有玩家");
        RegisterCommand("lock", HandleLockCommand, "锁定/解锁房间（仅房主）");
        
        context.Logger.Info($"已初始化，注册了 {_commands.Count} 个命令");
        await Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        _context?.Logger.Info("正在关闭");
        _commands.Clear();
        return Task.CompletedTask;
    }

    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        // 检查是否为命令（以 / 开头）
        if (!context.Message.StartsWith("/"))
            return;

        var parts = context.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;

        var commandName = parts[0][1..].ToLower(); // 移除 '/' 前缀
        var args = parts.Skip(1).ToArray();

        if (_commands.TryGetValue(commandName, out var handler))
        {
            try
            {
                _context?.Logger.Debug($"用户 {context.User.Name} 执行命令: {commandName}");
                await handler(context.Room, context.User, args);
            }
            catch (Exception ex)
            {
                _context?.Logger.Error(ex, $"执行命令 '{commandName}' 时出错");
                await _context!.API.SendRoomMessageAsync(context.Room, $"❌ 错误: {ex.Message}");
            }
        }
    }

    private void RegisterCommand(string name, CommandHandler handler, string description)
    {
        _commands[name.ToLower()] = handler;
    }

    /// <summary>
    /// 处理 /kick 命令
    /// </summary>
    private async Task HandleKickCommand(Room room, User user, string[] args)
    {
        // 仅房主可以踢人
        if (!room.IsHost(user))
        {
            await _context!.API.SendRoomMessageAsync(room, "❌ 只有房主可以使用 /kick 命令");
            return;
        }

        if (args.Length == 0)
        {
            await _context!.API.SendRoomMessageAsync(room, "💡 用法: /kick <用户名>");
            return;
        }

        var targetUsername = string.Join(" ", args);
        var targetUser = room.GetUsers().FirstOrDefault(u => 
            u.Name.Equals(targetUsername, StringComparison.OrdinalIgnoreCase));

        if (targetUser == null)
        {
            await _context!.API.SendRoomMessageAsync(room, $"❌ 用户 '{targetUsername}' 不在房间内");
            return;
        }

        if (room.IsHost(targetUser))
        {
            await _context!.API.SendRoomMessageAsync(room, "❌ 不能踢出房主");
            return;
        }

        _context!.Logger.Info($"用户 {user.Name} 踢出了 {targetUser.Name}");
        
        // 通知房间
        await _context.API.SendRoomMessageAsync(room, $"👢 {targetUser.Name} 被踢出了房间");
        
        // 踢出用户
        await _context.API.RemoveUserFromRoomAsync(targetUser, "你已被房主踢出房间");
    }

    /// <summary>
    /// 处理 /help 命令
    /// </summary>
    private async Task HandleHelpCommand(Room room, User user, string[] args)
    {
        var helpText = 
            "📖 命令插件帮助\n" +
            "━━━━━━━━━━━━━━━━\n" +
            "/kick <用户名> - 踢出用户（仅房主）\n" +
            "/list - 列出房间内所有玩家\n" +
            "/lock - 锁定/解锁房间（仅房主）\n" +
            "/help - 显示此帮助信息\n" +
            "/info - 显示插件信息";
        
        await _context!.API.SendRoomMessageAsync(room, helpText);
    }

    /// <summary>
    /// 处理 /info 命令
    /// </summary>
    private async Task HandleInfoCommand(Room room, User user, string[] args)
    {
        var info = 
            "ℹ️ 命令插件信息\n" +
            "━━━━━━━━━━━━━━━━\n" +
            "版本: 3.0 (基于 MEF + PluginAPI)\n" +
            $"已注册命令: {_commands.Count} 个\n" +
            $"插件目录: {_context?.PluginDirectory ?? "N/A"}\n" +
            $"当前在线: {_context?.API.GetOnlineUserCount() ?? 0} 人\n" +
            $"活跃房间: {_context?.API.GetActiveRoomCount() ?? 0} 个";
        
        await _context!.API.SendRoomMessageAsync(room, info);
    }

    /// <summary>
    /// 处理 /list 命令
    /// </summary>
    private async Task HandleListCommand(Room room, User user, string[] args)
    {
        var users = room.GetAllUsers().ToList();
        var userList = string.Join("\n", users.Select((u, i) => 
            $"{i + 1}. {u.Name}{(room.IsHost(u) ? " 👑" : "")}{(u.IsMonitor ? " 👁️" : "")}"));
        
        var message = 
            $"👥 房间玩家列表 ({users.Count} 人)\n" +
            "━━━━━━━━━━━━━━━━\n" +
            userList;
        
        await _context!.API.SendRoomMessageAsync(room, message);
    }

    /// <summary>
    /// 处理 /lock 命令
    /// </summary>
    private async Task HandleLockCommand(Room room, User user, string[] args)
    {
        // 仅房主可以锁定房间
        if (!room.IsHost(user))
        {
            await _context!.API.SendRoomMessageAsync(room, "❌ 只有房主可以锁定/解锁房间");
            return;
        }

        bool newLockState = !room.Locked;
        await _context!.API.SetRoomLockAsync(room, newLockState);
        
        var message = newLockState 
            ? "🔒 房间已锁定" 
            : "🔓 房间已解锁";
        
        await _context.API.SendRoomMessageAsync(room, message);
        _context.Logger.Info($"用户 {user.Name} {(newLockState ? "锁定" : "解锁")}了房间 {room.Id}");
    }
}
