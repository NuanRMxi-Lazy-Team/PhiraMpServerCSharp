using System.Collections.Concurrent;
using PhiraMp.Plugin.SDK;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// Server API implementation exposed to plugins
/// </summary>
public class ServerAPIImpl : IServerAPI
{
    private readonly ServerState _serverState;
    private readonly List<Func<IRoomContext, IUserContext, string, Task>> _roomMessageHandlers = new();
    private readonly List<Func<IRoomContext, string, Task>> _roomStateChangeHandlers = new();
    private readonly List<Func<IRoomContext, IUserContext, Task>> _userJoinHandlers = new();
    private readonly List<Func<IRoomContext, IUserContext, Task>> _userLeaveHandlers = new();
    private readonly object _lock = new();

    public ServerAPIImpl(ServerState serverState)
    {
        _serverState = serverState;
    }

    public void SubscribeToRoomMessages(Func<IRoomContext, IUserContext, string, Task> handler)
    {
        lock (_lock)
        {
            _roomMessageHandlers.Add(handler);
        }
    }

    public void UnsubscribeFromRoomMessages(Func<IRoomContext, IUserContext, string, Task> handler)
    {
        lock (_lock)
        {
            _roomMessageHandlers.Remove(handler);
        }
    }

    public void SubscribeToRoomStateChange(Func<IRoomContext, string, Task> handler)
    {
        lock (_lock)
        {
            _roomStateChangeHandlers.Add(handler);
        }
    }

    public void UnsubscribeFromRoomStateChange(Func<IRoomContext, string, Task> handler)
    {
        lock (_lock)
        {
            _roomStateChangeHandlers.Remove(handler);
        }
    }

    public void SubscribeToUserJoin(Func<IRoomContext, IUserContext, Task> handler)
    {
        lock (_lock)
        {
            _userJoinHandlers.Add(handler);
        }
    }

    public void UnsubscribeFromUserJoin(Func<IRoomContext, IUserContext, Task> handler)
    {
        lock (_lock)
        {
            _userJoinHandlers.Remove(handler);
        }
    }

    public void SubscribeToUserLeave(Func<IRoomContext, IUserContext, Task> handler)
    {
        lock (_lock)
        {
            _userLeaveHandlers.Add(handler);
        }
    }

    public void UnsubscribeFromUserLeave(Func<IRoomContext, IUserContext, Task> handler)
    {
        lock (_lock)
        {
            _userLeaveHandlers.Remove(handler);
        }
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

        List<Func<IRoomContext, IUserContext, string, Task>> handlers;
        lock (_lock)
        {
            handlers = new List<Func<IRoomContext, IUserContext, string, Task>>(_roomMessageHandlers);
        }

        foreach (var handler in handlers)
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

        List<Func<IRoomContext, string, Task>> handlers;
        lock (_lock)
        {
            handlers = new List<Func<IRoomContext, string, Task>>(_roomStateChangeHandlers);
        }

        foreach (var handler in handlers)
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

        List<Func<IRoomContext, IUserContext, Task>> handlers;
        lock (_lock)
        {
            handlers = new List<Func<IRoomContext, IUserContext, Task>>(_userJoinHandlers);
        }

        foreach (var handler in handlers)
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

        List<Func<IRoomContext, IUserContext, Task>> handlers;
        lock (_lock)
        {
            handlers = new List<Func<IRoomContext, IUserContext, Task>>(_userLeaveHandlers);
        }

        foreach (var handler in handlers)
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
