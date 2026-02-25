using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Language { get; set; }
    public ServerState Server { get; set; }
    public WeakReference<Session>? SessionRef { get; set; }
    public Room? Room { get; set; }
    public bool IsMonitor { get; set; }
    public float GameTime { get; set; } = float.NegativeInfinity;
    public object DangleMark { get; set; } = new();
    private bool _dangling;

    private readonly Lock _lock = new();

    public User(int id, string name, string language, ServerState server)
    {
        Id = id;
        Name = name;
        Language = language;
        Server = server;
    }

    public UserInfo ToInfo()
    {
        return new UserInfo(Id, Name, IsMonitor);
    }

    public bool CanMonitor()
    {
        return Server.Config.Monitors.Contains(Id);
    }

    public void SetSession(Session session)
    {
        lock (_lock)
        {
            SessionRef = new WeakReference<Session>(session);
            DangleMark = new object();
            _dangling = false;
        }
    }

    public async Task TrySendAsync(ServerCommand cmd)
    {
        Session? session = null;
        lock (_lock)
        {
            SessionRef?.TryGetTarget(out session);
        }

        if (session != null)
        {
            await session.TrySendAsync(cmd);
        }
    }

    public async Task DangleAsync()
    {
        lock (_lock)
        {
            if (_dangling) return;
            _dangling = true;
        }

        Logger.Warning($"User {Id} dangling");

        var room = Room;
        if (room?.State is InternalRoomState.Playing)
        {
            Logger.Warning($"User {Id} lost connection while playing, aborting");
            Server.Users.TryRemove(Id, out _);

            // Notify plugins that the user has disconnected
            if (Server.PluginManager != null)
            {
                try { await Server.PluginManager.DispatchUserDisconnectAsync(this); }
                catch (Exception ex) { Logger.Error(ex, $"Error dispatching disconnect for user {Id}:"); }
            }

            if (await room.OnUserLeaveAsync(this))
            {
                Server.Rooms.TryRemove(room.Id.Value, out _);
            }
            return;
        }

        var dangleMark = new object();
        var userId = Id;
        var server = Server;
        DangleMark = dangleMark;

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10));

            if (ReferenceEquals(DangleMark, dangleMark))
            {
                // Notify plugins that the user has disconnected (reconnect timeout elapsed)
                if (server.PluginManager != null)
                {
                    try { await server.PluginManager.DispatchUserDisconnectAsync(this); }
                    catch (Exception ex) { Logger.Error(ex, $"Error dispatching disconnect for user {userId}:"); }
                }

                var currentRoom = Room;
                if (currentRoom != null)
                {
                    server.Users.TryRemove(userId, out _);
                    if (await currentRoom.OnUserLeaveAsync(this))
                    {
                        server.Rooms.TryRemove(currentRoom.Id.Value, out _);
                    }
                }
                else
                {
                    // No room — still remove from users
                    server.Users.TryRemove(userId, out _);
                }
            }
        });
    }
}