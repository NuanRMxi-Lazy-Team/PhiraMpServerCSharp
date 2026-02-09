using System.Collections.Concurrent;
using PhiraMp.Plugin.SDK;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// Server API implementation exposed to plugins
/// </summary>
public class ServerAPIImpl : IServerAPI
{
    private readonly ServerState _serverState;
    private readonly ConcurrentBag<Func<IRoomContext, IUserContext, string, Task>> _roomMessageHandlers = new();
    private readonly ConcurrentBag<Func<IRoomContext, string, Task>> _roomStateChangeHandlers = new();
    private readonly ConcurrentBag<Func<IRoomContext, IUserContext, Task>> _userJoinHandlers = new();
    private readonly ConcurrentBag<Func<IRoomContext, IUserContext, Task>> _userLeaveHandlers = new();

    public ServerAPIImpl(ServerState serverState)
    {
        _serverState = serverState;
    }

    public void SubscribeToRoomMessages(Func<IRoomContext, IUserContext, string, Task> handler)
    {
        _roomMessageHandlers.Add(handler);
    }

    public void UnsubscribeFromRoomMessages(Func<IRoomContext, IUserContext, string, Task> handler)
    {
        // Note: ConcurrentBag doesn't support removal, but this is acceptable for plugin lifecycle
        // Handlers will be cleaned up when plugins are unloaded
    }

    public void SubscribeToRoomStateChange(Func<IRoomContext, string, Task> handler)
    {
        _roomStateChangeHandlers.Add(handler);
    }

    public void UnsubscribeFromRoomStateChange(Func<IRoomContext, string, Task> handler)
    {
        // Note: ConcurrentBag doesn't support removal
    }

    public void SubscribeToUserJoin(Func<IRoomContext, IUserContext, Task> handler)
    {
        _userJoinHandlers.Add(handler);
    }

    public void UnsubscribeFromUserJoin(Func<IRoomContext, IUserContext, Task> handler)
    {
        // Note: ConcurrentBag doesn't support removal
    }

    public void SubscribeToUserLeave(Func<IRoomContext, IUserContext, Task> handler)
    {
        _userLeaveHandlers.Add(handler);
    }

    public void UnsubscribeFromUserLeave(Func<IRoomContext, IUserContext, Task> handler)
    {
        // Note: ConcurrentBag doesn't support removal
    }

    public IEnumerable<IRoomContext> GetRooms()
    {
        return _serverState.Rooms.Values.Select(r => new RoomContextImpl(r));
    }

    public IRoomContext? GetRoom(string roomId)
    {
        if (_serverState.Rooms.TryGetValue(roomId, out var room))
        {
            return new RoomContextImpl(room);
        }
        return null;
    }

    // Internal methods to trigger events
    internal async Task OnRoomMessageAsync(Room room, User user, string message)
    {
        var roomContext = new RoomContextImpl(room);
        var userContext = new UserContextImpl(user, room);

        foreach (var handler in _roomMessageHandlers)
        {
            try
            {
                await handler(roomContext, userContext, message);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Plugin error in room message handler:");
            }
        }
    }

    internal async Task OnRoomStateChangeAsync(Room room, string newState)
    {
        var roomContext = new RoomContextImpl(room);

        foreach (var handler in _roomStateChangeHandlers)
        {
            try
            {
                await handler(roomContext, newState);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Plugin error in room state change handler:");
            }
        }
    }

    internal async Task OnUserJoinAsync(Room room, User user)
    {
        var roomContext = new RoomContextImpl(room);
        var userContext = new UserContextImpl(user, room);

        foreach (var handler in _userJoinHandlers)
        {
            try
            {
                await handler(roomContext, userContext);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Plugin error in user join handler:");
            }
        }
    }

    internal async Task OnUserLeaveAsync(Room room, User user)
    {
        var roomContext = new RoomContextImpl(room);
        var userContext = new UserContextImpl(user, room);

        foreach (var handler in _userLeaveHandlers)
        {
            try
            {
                await handler(roomContext, userContext);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Plugin error in user leave handler:");
            }
        }
    }
}

/// <summary>
/// Room context implementation
/// </summary>
internal class RoomContextImpl : IRoomContext
{
    private readonly Room _room;

    public RoomContextImpl(Room room)
    {
        _room = room;
    }

    public string RoomId => _room.Id.Value;
    public bool IsLocked => _room.Locked;
    public bool IsCycleMode => _room.Cycle;
    public bool IsCycleVotingMode => _room.CycleVotingMode;
    
    public IUserContext? Host => _room.Host != null ? new UserContextImpl(_room.Host, _room) : null;

    public IEnumerable<IUserContext> GetUsers()
    {
        return _room.GetUsers().Select(u => new UserContextImpl(u, _room));
    }

    public async Task SendMessageAsync(string message)
    {
        await _room.SendAsync(new Core.ChatMessage(-1, message));
    }

    public async Task SendMessageToUserAsync(IUserContext user, string message)
    {
        var userId = user.UserId;
        var actualUser = _room.GetAllUsers().FirstOrDefault(u => u.Id == userId);
        if (actualUser != null)
        {
            await actualUser.TrySendAsync(new Core.MessageCommand(new Core.ChatMessage(-1, message)));
        }
    }

    public async Task KickUserAsync(IUserContext user)
    {
        var userId = user.UserId;
        var actualUser = _room.GetAllUsers().FirstOrDefault(u => u.Id == userId);
        if (actualUser != null)
        {
            // Send a message to notify the user they are being kicked
            await actualUser.TrySendAsync(new Core.MessageCommand(new Core.ChatMessage(-1, "You have been kicked from the room")));
            
            // Trigger disconnection which will cause the user to leave the room
            await Task.Delay(100); // Give time for message to be sent
            
            // Close the session to force disconnect
            if (actualUser.SessionRef != null && actualUser.SessionRef.TryGetTarget(out var session))
            {
                session.Dispose();
            }
        }
    }
}

/// <summary>
/// User context implementation
/// </summary>
internal class UserContextImpl : IUserContext
{
    private readonly User _user;
    private readonly Room _room;

    public UserContextImpl(User user, Room room)
    {
        _user = user;
        _room = room;
    }

    public int UserId => _user.Id;
    public string UserName => _user.Name;
    public bool IsHost => _room.IsHost(_user);
    public bool IsMonitor => _user.IsMonitor;
}
