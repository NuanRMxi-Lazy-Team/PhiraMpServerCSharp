using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server;

public class Room : IRoom
{
    public RoomId Id { get; }
    public User Host { get; set; }
    public InternalRoomState State { get; set; } = new InternalRoomState.SelectChart();
    public bool Live { get; set; }
    public bool Locked { get; set; }
    public bool Cycle { get; private set; }
    public ChartInfo? Chart { get; set; }

    private readonly List<User> _users = new();
    private readonly List<User> _monitors = new();
    private readonly object _lock = new();
    private readonly int _maxUsers;
    private readonly ServerState? _serverState;

    // 显式实现 IRoom.Host，将内部 User 暴露为 IUser
    IUser IRoom.Host => Host;

    public Room(RoomId id, User host, int maxUsers = 8, ServerState? serverState = null)
    {
        Id = id;
        Host = host;
        _maxUsers = maxUsers;
        _serverState = serverState;
        _users.Add(host);
    }

    public bool IsHost(User user) => Host.Id == user.Id;

    public void CheckHost(User user)
    {
        if (!IsHost(user))
            throw new Exception("Only host can do this");
    }

    public void SetCycle(bool cycle)
    {
        Cycle = cycle;
    }

    public void CheckCanSelectChart(User user)
    {
        // Only host can select charts by default
        // Plugins can override this behavior
        CheckHost(user);
    }

    // ===== IRoom 接口显式实现（接受 IUser，内部转为 User）=====

    bool IRoom.IsHost(IUser user) => Host.Id == user.Id;

    void IRoom.CheckHost(IUser user)
    {
        if (Host.Id != user.Id)
            throw new Exception("Only host can do this");
    }

    void IRoom.CheckCanSelectChart(IUser user)
    {
        if (Host.Id != user.Id)
            throw new Exception("Only host can do this");
    }

    Task<bool> IRoom.OnUserLeaveAsync(IUser user) => OnUserLeaveAsync((User)user);

    bool IRoom.AddUser(IUser user, bool monitor) => AddUser((User)user, monitor);

    Task IRoom.SendAsAsync(IUser user, string content) => SendAsAsync((User)user, content);

    List<IUser> IRoom.GetUsers() => GetUsers().Cast<IUser>().ToList();
    List<IUser> IRoom.GetMonitors() => GetMonitors().Cast<IUser>().ToList();
    List<IUser> IRoom.GetAllUsers() => GetAllUsers().Cast<IUser>().ToList();

    public RoomStateData GetClientRoomState()
    {
        return State.ToClient(Chart?.Id);
    }

    public ClientRoomState ClientState(User user)
    {
        var users = GetAllUsers();
        var isReady = State is InternalRoomState.WaitForReady waitState &&
                      waitState.Started.Contains(user.Id);

        return new ClientRoomState(
            Id,
            GetClientRoomState(),
            Live,
            Locked,
            Cycle,
            IsHost(user),
            isReady,
            users.ToDictionary(u => u.Id, u => u.ToInfo())
        );
    }

    public async Task OnStateChangeAsync()
    {
        await BroadcastAsync(new ChangeStateCommand(GetClientRoomState()));
        
        // Notify plugins of state change
        if (_serverState?.PluginManager != null)
        {
            var stateName = State switch
            {
                InternalRoomState.SelectChart => "SelectChart",
                InternalRoomState.WaitForReady => "WaitingForReady",
                InternalRoomState.Playing => "Playing",
                _ => "Unknown"
            };
            await _serverState.PluginManager.DispatchRoomStateChangeAsync(this, stateName);
        }
    }

    public bool AddUser(User user, bool monitor)
    {
        lock (_lock)
        {
            if (monitor)
            {
                _monitors.RemoveAll(u => u == null);
                _monitors.Add(user);
                return true;
            }
            else
            {
                _users.RemoveAll(u => u == null);
                if (_users.Count >= _maxUsers)
                    return false;

                _users.Add(user);
                return true;
            }
        }
    }

    /// <summary>
    /// 获取所有用户
    /// </summary>
    /// <returns></returns>
    public List<User> GetUsers()
    {
        lock (_lock)
        {
            return _users.Where(u => u != null).ToList();
        }
    }

    /// <summary>
    /// 获取所有监视器
    /// </summary>
    public List<User> GetMonitors()
    {
        lock (_lock)
        {
            return _monitors.Where(u => u != null).ToList();
        }
    }

    /// <summary>
    /// 获取所有玩家（排除监视器）
    /// </summary>
    public List<User> GetAllUsers()
    {
        lock (_lock)
        {
            return _users.Concat(_monitors).Where(u => u != null).ToList();
        }
    }

    public async Task SendAsync(Message msg)
    {
        await BroadcastAsync(new MessageCommand(msg));
    }

    public async Task BroadcastAsync(ServerCommand cmd)
    {
        List<User> users;
        lock (_lock)
        {
            users = new List<User>(_users.Count + _monitors.Count);
            users.AddRange(_users.Where(u => u != null));
            users.AddRange(_monitors.Where(u => u != null));
        }

        if (users.Count == 0)
            return;

        if (users.Count == 1)
        {
            await users[0].TrySendAsync(cmd);
            return;
        }

        // Sequential sends to avoid Task allocation overhead
        // This is more memory-efficient than Task.WhenAll for large broadcasts
        for (int i = 0; i < users.Count; i++)
        {
            _ = users[i].TrySendAsync(cmd); // Fire and forget
        }
    }

    public async Task BroadcastMonitorsAsync(ServerCommand cmd)
    {
        List<User> monitors;
        lock (_lock)
        {
            monitors = new List<User>(_monitors.Count);
            monitors.AddRange(_monitors.Where(u => u != null));
        }

        if (monitors.Count == 0)
            return;

        if (monitors.Count == 1)
        {
            await monitors[0].TrySendAsync(cmd);
            return;
        }

        // Fire and forget for monitors (no need to wait for all)
        for (int i = 0; i < monitors.Count; i++)
        {
            _ = monitors[i].TrySendAsync(cmd);
        }
    }

    public async Task SendAsAsync(User user, string content)
    {
        await SendAsync(new ChatMessage(user.Id, content));
    }

    public async Task<bool> OnUserLeaveAsync(User user)
    {
        await SendAsync(new LeaveRoomMessage(user.Id, user.Name));
        
        // Notify plugins of user leaving
        if (_serverState?.PluginManager != null)
        {
            await _serverState.PluginManager.DispatchUserLeaveAsync(this, user);
        }
        
        user.Room = null;

        lock (_lock)
        {
            if (user.IsMonitor)
                _monitors.Remove(user);
            else
                _users.Remove(user);
        }

        if (IsHost(user))
        {
            var users = GetUsers();
            if (users.Count == 0)
            {
                return true; // Drop room
            }
            else
            {
                // Select new host randomly
                var newHost = users[Random.Shared.Next(users.Count)];
                Host = newHost;

                await SendAsync(new NewHostMessage(newHost.Id));
                await newHost.TrySendAsync(new ChangeHostCommand(true));
            }
        }

        await CheckAllReadyAsync();
        return false;
    }

    public void ResetGameTime()
    {
        var users = GetUsers();
        foreach (var user in users)
        {
            user.GameTime = float.NegativeInfinity;
        }
    }

    public async Task CheckAllReadyAsync()
    {
        switch (State)
        {
            case InternalRoomState.WaitForReady waitState:
            {
                var allUsers = GetAllUsers();
                if (allUsers.All(u => waitState.Started.Contains(u.Id)))
                {
                    Logger.Info($"Room {Id} game start");
                    await SendAsync(new StartPlayingMessage());
                    ResetGameTime();

                    State = new InternalRoomState.Playing
                    {
                        Results = new Dictionary<int, RecordInfo>(),
                        Aborted = new HashSet<int>()
                    };

                    await OnStateChangeAsync();
                }

                break;
            }

            case InternalRoomState.Playing playingState:
            {
                var users = GetUsers();
                if (users.All(u => playingState.Results.ContainsKey(u.Id) ||
                                   playingState.Aborted.Contains(u.Id)))
                {
                    await SendAsync(new GameEndMessage());
                    State = new InternalRoomState.SelectChart();

                    // Clear chart to prevent memory leaks
                    Chart = null;

                    if (Cycle)
                    {
                        Logger.Debug($"Room {Id} cycling");

                        var userList = users;
                        var currentHostIndex = userList.FindIndex(u => u.Id == Host.Id);
                        var nextHostIndex = (currentHostIndex + 1) % userList.Count;
                        var newHost = userList[nextHostIndex];

                        var oldHost = Host;
                        Host = newHost;

                        await SendAsync(new NewHostMessage(newHost.Id));
                        await oldHost.TrySendAsync(new ChangeHostCommand(false));
                        await newHost.TrySendAsync(new ChangeHostCommand(true));
                    }

                    await OnStateChangeAsync();
                }

                break;
            }
        }
    }
}