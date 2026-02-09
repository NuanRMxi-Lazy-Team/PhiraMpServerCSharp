using PhiraMp.Plugin.SDK;

namespace PhiraMp.Plugins.CommandPlugin;

[Plugin("CommandPlugin", "1.0.0", "PhiraMP Team", "Provides command functionality like /kick")]
public class CommandPlugin : PluginBase
{
    private readonly Dictionary<string, Func<IRoomContext, IUserContext, string[], Task>> _commands = new();

    public override string Name => "CommandPlugin";
    public override string Version => "1.0.0";

    public override async Task OnLoadAsync(IPluginContext context)
    {
        await base.OnLoadAsync(context);

        // Register commands
        RegisterCommand("kick", HandleKickCommand);
        RegisterCommand("help", HandleHelpCommand);

        // Subscribe to room messages
        context.ServerAPI.SubscribeToRoomMessages(OnRoomMessage);

        Context.Logger.Info("CommandPlugin loaded successfully");
    }

    public override Task OnUnloadAsync()
    {
        Context.Logger.Info("CommandPlugin unloading");
        return Task.CompletedTask;
    }

    private void RegisterCommand(string name, Func<IRoomContext, IUserContext, string[], Task> handler)
    {
        _commands[name.ToLower()] = handler;
    }

    private async Task OnRoomMessage(IRoomContext room, IUserContext user, string message)
    {
        // Check if message is a command (starts with /)
        if (!message.StartsWith("/"))
            return;

        var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;

        var commandName = parts[0].Substring(1).ToLower(); // Remove the '/' prefix
        var args = parts.Skip(1).ToArray();

        if (_commands.TryGetValue(commandName, out var handler))
        {
            try
            {
                await handler(room, user, args);
            }
            catch (Exception ex)
            {
                Context.Logger.Error(ex, $"Error executing command '{commandName}':");
                await room.SendMessageToUserAsync(user, $"Error executing command: {ex.Message}");
            }
        }
    }

    private async Task HandleKickCommand(IRoomContext room, IUserContext user, string[] args)
    {
        // Only host can kick
        if (!user.IsHost)
        {
            await room.SendMessageToUserAsync(user, "Only the host can use the /kick command");
            return;
        }

        if (args.Length == 0)
        {
            await room.SendMessageToUserAsync(user, "Usage: /kick <username>");
            return;
        }

        var targetUsername = string.Join(" ", args);
        var targetUser = room.GetUsers().FirstOrDefault(u => 
            u.UserName.Equals(targetUsername, StringComparison.OrdinalIgnoreCase));

        if (targetUser == null)
        {
            await room.SendMessageToUserAsync(user, $"User '{targetUsername}' not found in room");
            return;
        }

        if (targetUser.IsHost)
        {
            await room.SendMessageToUserAsync(user, "Cannot kick the host");
            return;
        }

        Context.Logger.Info($"User {user.UserName} kicked {targetUser.UserName} from room {room.RoomId}");
        
        // Notify room
        await room.SendMessageAsync($"[System] {targetUser.UserName} has been kicked from the room");
        
        // Kick the user
        await room.KickUserAsync(targetUser);
    }

    private async Task HandleHelpCommand(IRoomContext room, IUserContext user, string[] args)
    {
        var helpText = "Available commands:\n" +
                       "/kick <username> - Kick a user from the room (host only)\n" +
                       "/help - Show this help message";
        
        await room.SendMessageToUserAsync(user, helpText);
    }
}
