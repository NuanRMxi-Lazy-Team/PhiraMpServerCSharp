using System.ComponentModel.Composition;
using PhiraMp.Server;
using PhiraMp.Server.Plugins;

namespace PhiraMp.Plugins.CommandPlugin;

/// <summary>
/// Command plugin using MEF - no forced base class or SDK dependency
/// Exports both IPluginModule and IRoomMessageHandler
/// </summary>
[Export(typeof(IPluginModule))]
[Export(typeof(IRoomMessageHandler))]
public class CommandPlugin : IPluginModule, IRoomMessageHandler
{
    private readonly Dictionary<string, Func<Room, User, string[], Task>> _commands = new();
    private PluginContext? _context;

    public async Task InitializeAsync(PluginContext context)
    {
        _context = context;
        
        // Register commands
        RegisterCommand("kick", HandleKickCommand);
        RegisterCommand("help", HandleHelpCommand);
        RegisterCommand("info", HandleInfoCommand);
        
        Console.WriteLine("[CommandPlugin] Initialized with MEF - No SDK required!");
        await Task.CompletedTask;
    }

    public Task ShutdownAsync()
    {
        Console.WriteLine("[CommandPlugin] Shutting down");
        _commands.Clear();
        return Task.CompletedTask;
    }

    public async Task HandleMessageAsync(RoomMessageContext context)
    {
        // Check if message is a command (starts with /)
        if (!context.Message.StartsWith("/"))
            return;

        var parts = context.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;

        var commandName = parts[0].Substring(1).ToLower(); // Remove the '/' prefix
        var args = parts.Skip(1).ToArray();

        if (_commands.TryGetValue(commandName, out var handler))
        {
            try
            {
                await handler(context.Room, context.User, args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CommandPlugin] Error executing command '{commandName}': {ex.Message}");
                await context.Room.SendAsync(new Core.ChatMessage(-1, $"Error: {ex.Message}"));
            }
        }
    }

    private void RegisterCommand(string name, Func<Room, User, string[], Task> handler)
    {
        _commands[name.ToLower()] = handler;
    }

    private async Task HandleKickCommand(Room room, User user, string[] args)
    {
        // Only host can kick
        if (!room.IsHost(user))
        {
            await room.SendAsync(new Core.ChatMessage(-1, "Only the host can use the /kick command"));
            return;
        }

        if (args.Length == 0)
        {
            await room.SendAsync(new Core.ChatMessage(-1, "Usage: /kick <username>"));
            return;
        }

        var targetUsername = string.Join(" ", args);
        var targetUser = room.GetUsers().FirstOrDefault(u => 
            u.Name.Equals(targetUsername, StringComparison.OrdinalIgnoreCase));

        if (targetUser == null)
        {
            await room.SendAsync(new Core.ChatMessage(-1, $"User '{targetUsername}' not found in room"));
            return;
        }

        if (room.IsHost(targetUser))
        {
            await room.SendAsync(new Core.ChatMessage(-1, "Cannot kick the host"));
            return;
        }

        Console.WriteLine($"[CommandPlugin] User {user.Name} kicked {targetUser.Name} from room {room.Id}");
        
        // Notify room
        await room.SendAsync(new Core.ChatMessage(-1, $"[System] {targetUser.Name} has been kicked from the room"));
        
        // Kick the user by closing their session
        if (targetUser.SessionRef != null && targetUser.SessionRef.TryGetTarget(out var session))
        {
            await room.SendAsync(new Core.ChatMessage(-1, "You have been kicked from the room"));
            await Task.Delay(100);
            session.Dispose();
        }
    }

    private async Task HandleHelpCommand(Room room, User user, string[] args)
    {
        var helpText = "[Command Plugin Help]\n" +
                       "/kick <username> - Kick a user from the room (host only)\n" +
                       "/help - Show this help message\n" +
                       "/info - Show plugin information";
        
        await room.SendAsync(new Core.ChatMessage(-1, helpText));
    }

    private async Task HandleInfoCommand(Room room, User user, string[] args)
    {
        var info = "[CommandPlugin]\n" +
                   "Version: 2.0 (MEF-based)\n" +
                   "No SDK required - uses MEF for discovery\n" +
                   $"Registered commands: {_commands.Count}\n" +
                   $"Plugin directory: {_context?.PluginDirectory ?? "N/A"}";
        
        await room.SendAsync(new Core.ChatMessage(-1, info));
    }
}
