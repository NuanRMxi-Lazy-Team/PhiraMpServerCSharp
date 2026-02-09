using System.ComponentModel.Composition;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// Optional marker interface for plugins. Plugins don't need to implement this.
/// Use [Export(typeof(IPluginModule))] to be discovered by MEF.
/// </summary>
public interface IPluginModule
{
    /// <summary>
    /// Called when plugin is loaded. Optional - implement if needed.
    /// </summary>
    Task InitializeAsync(PluginContext context);
    
    /// <summary>
    /// Called when plugin is unloaded. Optional - implement if needed.
    /// </summary>
    Task ShutdownAsync();
}

/// <summary>
/// Interface for room message handlers. Export this to handle room messages.
/// Use [Export(typeof(IRoomMessageHandler))] to register.
/// </summary>
public interface IRoomMessageHandler
{
    Task HandleMessageAsync(RoomMessageContext context);
}

/// <summary>
/// Interface for room state change handlers. Export this to handle state changes.
/// Use [Export(typeof(IRoomStateHandler))] to register.
/// </summary>
public interface IRoomStateHandler
{
    Task HandleStateChangeAsync(RoomStateContext context);
}

/// <summary>
/// Interface for user join handlers. Export this to handle user joins.
/// Use [Export(typeof(IUserJoinHandler))] to register.
/// </summary>
public interface IUserJoinHandler
{
    Task HandleUserJoinAsync(UserEventContext context);
}

/// <summary>
/// Interface for user leave handlers. Export this to handle user leaves.
/// Use [Export(typeof(IUserLeaveHandler))] to register.
/// </summary>
public interface IUserLeaveHandler
{
    Task HandleUserLeaveAsync(UserEventContext context);
}

/// <summary>
/// Context provided to plugins during initialization
/// </summary>
public class PluginContext
{
    public ServerState ServerState { get; }
    public string PluginDirectory { get; }
    public string ConfigDirectory { get; }
    public string DataDirectory { get; }
    
    public PluginContext(ServerState serverState, string pluginDir, string configDir, string dataDir)
    {
        ServerState = serverState;
        PluginDirectory = pluginDir;
        ConfigDirectory = configDir;
        DataDirectory = dataDir;
    }
}

/// <summary>
/// Context for room message events
/// </summary>
public class RoomMessageContext
{
    public Room Room { get; }
    public User User { get; }
    public string Message { get; }
    
    public RoomMessageContext(Room room, User user, string message)
    {
        Room = room;
        User = user;
        Message = message;
    }
}

/// <summary>
/// Context for room state change events
/// </summary>
public class RoomStateContext
{
    public Room Room { get; }
    public string NewState { get; }
    
    public RoomStateContext(Room room, string newState)
    {
        Room = room;
        NewState = newState;
    }
}

/// <summary>
/// Context for user join/leave events
/// </summary>
public class UserEventContext
{
    public Room Room { get; }
    public User User { get; }
    
    public UserEventContext(Room room, User user)
    {
        Room = room;
        User = user;
    }
}
