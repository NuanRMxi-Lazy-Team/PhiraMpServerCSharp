namespace PhiraMp.Plugin.SDK;

/// <summary>
/// Plugin context interface providing access to server APIs
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Logger for plugin
    /// </summary>
    IPluginLogger Logger { get; }

    /// <summary>
    /// Server API
    /// </summary>
    IServerAPI ServerAPI { get; }

    /// <summary>
    /// Plugin configuration directory
    /// </summary>
    string ConfigDirectory { get; }

    /// <summary>
    /// Plugin data directory
    /// </summary>
    string DataDirectory { get; }
}

/// <summary>
/// Logger interface for plugins
/// </summary>
public interface IPluginLogger
{
    void Info(string message);
    void Debug(string message);
    void Warning(string message);
    void Error(string message);
    void Error(Exception ex, string message);
}

/// <summary>
/// Server API interface exposed to plugins
/// </summary>
public interface IServerAPI
{
    /// <summary>
    /// Subscribe to room message events
    /// </summary>
    void SubscribeToRoomMessages(Func<IRoomContext, IUserContext, string, Task> handler);

    /// <summary>
    /// Unsubscribe from room message events
    /// </summary>
    void UnsubscribeFromRoomMessages(Func<IRoomContext, IUserContext, string, Task> handler);

    /// <summary>
    /// Subscribe to room state change events
    /// </summary>
    void SubscribeToRoomStateChange(Func<IRoomContext, string, Task> handler);

    /// <summary>
    /// Unsubscribe from room state change events
    /// </summary>
    void UnsubscribeFromRoomStateChange(Func<IRoomContext, string, Task> handler);

    /// <summary>
    /// Subscribe to user join events
    /// </summary>
    void SubscribeToUserJoin(Func<IRoomContext, IUserContext, Task> handler);

    /// <summary>
    /// Unsubscribe from user join events
    /// </summary>
    void UnsubscribeFromUserJoin(Func<IRoomContext, IUserContext, Task> handler);

    /// <summary>
    /// Subscribe to user leave events
    /// </summary>
    void SubscribeToUserLeave(Func<IRoomContext, IUserContext, Task> handler);

    /// <summary>
    /// Unsubscribe from user leave events
    /// </summary>
    void UnsubscribeFromUserLeave(Func<IRoomContext, IUserContext, Task> handler);

    /// <summary>
    /// Get all rooms
    /// </summary>
    IEnumerable<IRoomContext> GetRooms();

    /// <summary>
    /// Get room by ID
    /// </summary>
    IRoomContext? GetRoom(string roomId);
}

/// <summary>
/// Room context interface for plugins
/// </summary>
public interface IRoomContext
{
    string RoomId { get; }
    bool IsLocked { get; }
    bool IsCycleMode { get; }
    bool IsCycleVotingMode { get; }
    IUserContext? Host { get; }
    IEnumerable<IUserContext> GetUsers();
    Task SendMessageAsync(string message);
    Task SendMessageToUserAsync(IUserContext user, string message);
    Task KickUserAsync(IUserContext user);
}

/// <summary>
/// User context interface for plugins
/// </summary>
public interface IUserContext
{
    int UserId { get; }
    string UserName { get; }
    bool IsHost { get; }
    bool IsMonitor { get; }
}
