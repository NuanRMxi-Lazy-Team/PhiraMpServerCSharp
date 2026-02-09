using System.ComponentModel.Composition;
using PhiraMp.Server.Models;

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
/// Interface for request start handlers. Export this to validate/modify start requests.
/// Use [Export(typeof(IRequestStartHandler))] to register.
/// Plugins can throw exceptions to prevent game start.
/// </summary>
public interface IRequestStartHandler
{
    Task HandleRequestStartAsync(RequestStartContext context);
}

/// <summary>
/// Interface for select chart handlers. Export this to handle chart selection.
/// Use [Export(typeof(ISelectChartHandler))] to register.
/// </summary>
public interface ISelectChartHandler
{
    Task HandleSelectChartAsync(SelectChartContext context);
}

/// <summary>
/// Interface for cycle mode change handlers. Export this to handle cycle mode changes.
/// Use [Export(typeof(ICycleModeChangeHandler))] to register.
/// </summary>
public interface ICycleModeChangeHandler
{
    Task HandleCycleModeChangeAsync(CycleModeChangeContext context);
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

/// <summary>
/// Context for request start events
/// </summary>
public class RequestStartContext
{
    public Room Room { get; }
    public User User { get; }
    
    public RequestStartContext(Room room, User user)
    {
        Room = room;
        User = user;
    }
}

/// <summary>
/// Context for select chart events
/// </summary>
public class SelectChartContext
{
    public Room Room { get; }
    public User User { get; }
    public ChartInfo Chart { get; }
    
    public SelectChartContext(Room room, User user, ChartInfo chart)
    {
        Room = room;
        User = user;
        Chart = chart;
    }
}

/// <summary>
/// Context for cycle mode change events
/// </summary>
public class CycleModeChangeContext
{
    public Room Room { get; }
    public User User { get; }
    public bool CycleEnabled { get; }
    
    public CycleModeChangeContext(Room room, User user, bool cycleEnabled)
    {
        Room = room;
        User = user;
        CycleEnabled = cycleEnabled;
    }
}

