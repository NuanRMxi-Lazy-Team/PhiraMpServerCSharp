using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhiraMpServer.Common;

namespace PhiraMpServer.Server;

public class ChartInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class RecordInfo
{
    public int Id { get; set; }
    public int Player { get; set; }
    public int Score { get; set; }
    public int Perfect { get; set; }
    public int Good { get; set; }
    public int Bad { get; set; }
    public int Miss { get; set; }
    public int MaxCombo { get; set; }
    public float Accuracy { get; set; }
    public bool FullCombo { get; set; }
    public float Std { get; set; }
    public float StdScore { get; set; }
}

public abstract record InternalRoomState
{
    public record SelectChart : InternalRoomState;

    public record WaitForReady : InternalRoomState
    {
        public HashSet<int> Started { get; init; } = new();
    }

    public record Playing : InternalRoomState
    {
        public Dictionary<int, RecordInfo> Results { get; init; } = new();
        public HashSet<int> Aborted { get; init; } = new();
    }

    public RoomStateData ToClient(int? chartId)
    {
        return this switch
        {
            SelectChart => new RoomStateData(RoomState.SelectChart, chartId),
            WaitForReady => new RoomStateData(RoomState.WaitingForReady, null),
            Playing => new RoomStateData(RoomState.Playing, null),
            _ => throw new InvalidOperationException()
        };
    }
}

public class Room
{
    public RoomId Id { get; }
    public User Host { get; set; }
    public InternalRoomState State { get; set; } = new InternalRoomState.SelectChart();
    public bool Live { get; set; }
    public bool Locked { get; set; }
    public bool Cycle { get; set; }
    public ChartInfo? Chart { get; set; }

    private readonly List<User> _users = new();
    private readonly List<User> _monitors = new();
    private readonly object _lock = new();
    private readonly int _maxUsers;

    public Room(RoomId id, User host, int maxUsers = 8)
    {
        Id = id;
        Host = host;
        _maxUsers = maxUsers;
        _users.Add(host);
    }

    public bool IsHost(User user) => Host.Id == user.Id;

    public void CheckHost(User user)
    {
        if (!IsHost(user))
            throw new Exception("Only host can do this");
    }

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

    public List<User> GetUsers()
    {
        lock (_lock)
        {
            return _users.Where(u => u != null).ToList();
        }
    }

    public List<User> GetMonitors()
    {
        lock (_lock)
        {
            return _monitors.Where(u => u != null).ToList();
        }
    }

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
        var users = GetAllUsers();
        foreach (var user in users)
        {
            await user.TrySendAsync(cmd);
        }
    }

    public async Task BroadcastMonitorsAsync(ServerCommand cmd)
    {
        var monitors = GetMonitors();
        foreach (var user in monitors)
        {
            await user.TrySendAsync(cmd);
        }
    }

    public async Task SendAsAsync(User user, string content)
    {
        await SendAsync(new ChatMessage(user.Id, content));
    }

    public async Task<bool> OnUserLeaveAsync(User user)
    {
        await SendAsync(new LeaveRoomMessage(user.Id, user.Name));
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

        await CheckAllReadyAsync(null);
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

    public async Task CheckAllReadyAsync(ILogger? logger)
    {
        switch (State)
        {
            case InternalRoomState.WaitForReady waitState:
            {
                var allUsers = GetAllUsers();
                if (allUsers.All(u => waitState.Started.Contains(u.Id)))
                {
                    logger?.LogInformation("Room {RoomId} game start", Id);
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

                    if (Cycle)
                    {
                        logger?.LogDebug("Room {RoomId} cycling", Id);

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
