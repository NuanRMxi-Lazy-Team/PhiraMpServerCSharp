using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Net.Sockets;
using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server;

public class Session : IDisposable
{
    private const string PhiraHost = "https://phira.5wyxi.com";

    // Shared HttpClient instance to avoid port exhaustion
    private static readonly HttpClient SharedHttpClient = CreateSharedHttpClient();

    // Authentication token cache with shorter TTL (1 minute) for memory efficiency
    private static readonly ConcurrentDictionary<string, (PhiraUserInfo info, long expireTicks)> AuthTokenCache = new();
    private const long AuthCacheTtlTicks = 1L * 60L * 10_000_000L; // 1 minute in ticks (shorter to avoid memory bloat)

    // HTTP request timeout (seconds)
    private const int HttpTimeoutSeconds = 10;

    public Guid Id { get; }
    public ClientStream Stream { get; private set; } = null!;
    public User User { get; private set; } = null!;
    public ServerState Server { get; }
    private readonly CancellationTokenSource _cts;
    private readonly Task _monitorTask;
    private bool _authenticated;
    private bool _disposed;

    private Session(Guid id, ClientStream stream, ServerState server)
    {
        Id = id;
        Stream = stream;
        Server = server;
        _cts = new CancellationTokenSource();
        _monitorTask = Task.Run(MonitorHeartbeat);
    }

    public static async Task<Session> CreateAsync(
        Guid id,
        TcpClient client,
        ServerState server)
    {
        var session = new Session(id, null!, server);

        var stream = new ClientStream(
            client,
            cmd => session.HandleCommandAsync(cmd));

        typeof(Session).GetProperty(nameof(Stream))!.SetValue(session, stream);
        await Task.CompletedTask;
        return session;
    }

    private static HttpClient CreateSharedHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            MaxConnectionsPerServer = 100,
            EnableMultipleHttp2Connections = true
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds)
        };

        // Start cleanup task for expired tokens
        _ = Task.Run(CleanupExpiredTokens);

        return client;
    }

    private static async Task CleanupExpiredTokens()
    {
        try
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(10)); // Clean up every 10 seconds (more aggressive)

                var now = DateTime.UtcNow.Ticks;
                // Collect expired tokens
                var expiredTokens =
                    (from kvp in AuthTokenCache where kvp.Value.expireTicks < now select kvp.Key).ToList();

                // Remove them
                foreach (var token in expiredTokens)
                {
                    AuthTokenCache.TryRemove(token, out _);
                }

                if (expiredTokens.Count > 0)
                {
                    Logger.Debug(
                        $"Cleaned up {expiredTokens.Count} expired auth tokens, cache size: {AuthTokenCache.Count}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error in token cleanup task:");
        }
    }

    private async Task MonitorHeartbeat()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                // Check every 5 seconds instead of 1 second to reduce CPU usage
                await Task.Delay(TimeSpan.FromSeconds(5), _cts.Token);

                var lastRecv = Stream.LastReceive;
                if (DateTime.UtcNow - lastRecv > TimeSpan.FromSeconds(30))
                {
                    Logger.Warning($"Session {Id} heartbeat timeout");
                    await Server.LostConnectionAsync(Id);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
    }

    public async Task TrySendAsync(ServerCommand cmd)
    {
        try
        {
            await Stream.SendAsync(cmd);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to deliver command to {Id}:");
        }
    }

    private async Task<ServerCommand?> HandleCommandAsync(ClientCommand cmd)
    {
        try
        {
            if (!_authenticated)
            {
                if (cmd is AuthenticateCommand authCmd)
                {
                    return await AuthenticateAsync(authCmd);
                }
                else
                {
                    Logger.Warning($"Packet before authentication, ignoring: {cmd.GetType().Name}");
                    return null;
                }
            }

            return await ProcessCommandAsync(cmd);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error processing command {cmd.GetType().Name}:");
            return null;
        }
    }

    private async Task<ServerCommand> AuthenticateAsync(AuthenticateCommand cmd)
    {
        try
        {
            var token = cmd.Token.Value;
            if (token.Length > 32)
                return new AuthenticateResponseCommand("Invalid token");

            Logger.Debug($"Session {Id}: authenticate {token}");

            // Check cache first
            if (AuthTokenCache.TryGetValue(token, out var cached))
            {
                if (DateTime.UtcNow.Ticks < cached.expireTicks)
                {
                    // Cache hit - use cached result
                    var userInfo = cached.info;
                    return ProcessAuthenticatedUser(userInfo);
                }
                else
                {
                    // Cache expired - remove it
                    AuthTokenCache.TryRemove(token, out _);
                }
            }

            // Cache miss - fetch from server
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{PhiraHost}/me");
            request.Headers.Add("Authorization", $"Bearer {token}");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(HttpTimeoutSeconds));
            var response = await SharedHttpClient.SendAsync(request, cts.Token);
            response.EnsureSuccessStatusCode();

            var userInfo2 = await response.Content.ReadFromJsonAsync<PhiraUserInfo>(cancellationToken: cts.Token);
            if (userInfo2 == null)
            {
                return new AuthenticateResponseCommand("Failed to fetch user info");
            }

            // Cache the result
            AuthTokenCache[token] = (userInfo2, DateTime.UtcNow.Ticks + AuthCacheTtlTicks);

            Logger.Debug($"Session {Id} <- User: {userInfo2.Id}, Name: {userInfo2.Name}");
            return ProcessAuthenticatedUser(userInfo2);
        }
        catch (OperationCanceledException)
        {
            Logger.Warning($"Authentication timeout for session {Id}");
            return new AuthenticateResponseCommand("Authentication timeout");
        }
        catch (Exception ex)
        {
            Logger.Warning($"Failed to authenticate: {ex.Message}");
            return new AuthenticateResponseCommand(ex.Message);
        }
    }

    private ServerCommand ProcessAuthenticatedUser(PhiraUserInfo userInfo)
    {
        User? user;
        if (Server.Users.TryGetValue(userInfo.Id, out user))
        {
            Logger.Info($"User {userInfo.Id} reconnect");
            User = user;
            user.SetSession(this);
        }
        else
        {
            user = new User(userInfo.Id, userInfo.Name, userInfo.Language, Server);
            User = user;
            user.SetSession(this);
            Server.Users[userInfo.Id] = user;
        }

        _authenticated = true;

        var roomState = user.Room?.ClientState(user);
        return new AuthenticateResponseCommand(user.ToInfo(), roomState);
    }

    private async Task<ServerCommand?> ProcessCommandAsync(ClientCommand cmd)
    {
        return cmd switch
        {
            ChatCommand chatCmd => await HandleChatAsync(chatCmd),
            TouchesCommand touchesCmd => await HandleTouchesAsync(touchesCmd),
            JudgesCommand judgesCmd => await HandleJudgesAsync(judgesCmd),
            CreateRoomCommand createCmd => await HandleCreateRoomAsync(createCmd),
            JoinRoomCommand joinCmd => await HandleJoinRoomAsync(joinCmd),
            LeaveRoomCommand => await HandleLeaveRoomAsync(),
            LockRoomCommand lockCmd => await HandleLockRoomAsync(lockCmd),
            CycleRoomCommand cycleCmd => await HandleCycleRoomAsync(cycleCmd),
            SelectChartCommand selectCmd => await HandleSelectChartAsync(selectCmd),
            RequestStartCommand => await HandleRequestStartAsync(),
            ReadyCommand => await HandleReadyAsync(),
            CancelReadyCommand => await HandleCancelReadyAsync(),
            PlayedCommand playedCmd => await HandlePlayedAsync(playedCmd),
            AbortCommand => await HandleAbortAsync(),
            _ => null
        };
    }

    private async Task<ServerCommand> HandleChatAsync(ChatCommand cmd)
    {
        try
        {
            var room = User.Room ?? throw new Exception("No room");
            
            // Notify plugins before sending message
            await Server.ServerAPI.OnRoomMessageAsync(room, User, cmd.Message.Value);
            
            await room.SendAsAsync(User, cmd.Message.Value);
            return new ChatResponseCommand(true);
        }
        catch (Exception ex)
        {
            return new ChatResponseCommand(false, ex.Message);
        }
    }

    private async Task<ServerCommand?> HandleTouchesAsync(TouchesCommand cmd)
    {
        var room = User.Room;
        if (room != null && room.Live)
        {
            Logger.Debug($"Received {cmd.Frames.Count} touch events from {User.Id}");
            if (cmd.Frames.Count > 0)
            {
                var lastFrame = cmd.Frames[^1];
                User.GameTime = lastFrame.Time;
            }

            _ = Task.Run(() => room.BroadcastMonitorsAsync(new ServerTouchesCommand(User.Id, cmd.Frames)));
        }

        await Task.CompletedTask;
        return null;
    }

    private async Task<ServerCommand?> HandleJudgesAsync(JudgesCommand cmd)
    {
        var room = User.Room;
        if (room != null && room.Live)
        {
            Logger.Debug($"Received {cmd.Judges.Count} judge events from {User.Id}");
            _ = Task.Run(() => room.BroadcastMonitorsAsync(new ServerJudgesCommand(User.Id, cmd.Judges)));
        }

        await Task.CompletedTask;
        return null;
    }

    private async Task<ServerCommand> HandleCreateRoomAsync(CreateRoomCommand cmd)
    {
        try
        {
            if (User.Room != null)
                throw new Exception("Already in room");

            var room = new Room(cmd.Id, User, Server.Config.RoomMaxPlayers, Server.Config.CycleVotingMode, Server);
            if (!Server.Rooms.TryAdd(cmd.Id.Value, room))
            {
                throw new Exception("Room ID already occupied");
            }

            // Broadcast user info so clients can map user ID to username
            await room.BroadcastAsync(new OnJoinRoomCommand(User.ToInfo()));
            await Task.Delay(1); // Ensure message order
            await room.SendAsync(new CreateRoomMessage(User.Id));
            User.Room = room;

            Logger.Info($"User {User.Id} created room {cmd.Id}");
            return new CreateRoomResponseCommand(true);
        }
        catch (Exception ex)
        {
            return new CreateRoomResponseCommand(false, ex.Message);
        }
    }

    private async Task<ServerCommand> HandleJoinRoomAsync(JoinRoomCommand cmd)
    {
        try
        {
            if (User.Room != null)
                throw new Exception("Already in room");

            if (!Server.Rooms.TryGetValue(cmd.Id.Value, out var room))
                throw new Exception("Room not found");

            if (room.Locked)
                throw new Exception("Room is locked");

            if (room.State is not InternalRoomState.SelectChart)
                throw new Exception("Game ongoing");

            if (cmd.Monitor && !User.CanMonitor())
                throw new Exception("Cannot monitor");

            if (!room.AddUser(User, cmd.Monitor))
                throw new Exception("Room is full");

            Logger.Info($"User {User.Id} joined room {cmd.Id} (monitor: {cmd.Monitor})");

            User.IsMonitor = cmd.Monitor;
            if (cmd.Monitor && !room.Live)
            {
                room.Live = true;
                Logger.Info($"Room {cmd.Id} goes live");
            }

            await room.BroadcastAsync(new OnJoinRoomCommand(User.ToInfo()));
            await room.SendAsync(new JoinRoomMessage(User.Id, User.Name));
            User.Room = room;

            // Notify plugins of user joining
            await Server.ServerAPI.OnUserJoinAsync(room, User);

            var response = new JoinRoomResponse(
                room.GetClientRoomState(),
                room.GetAllUsers().Select(u => u.ToInfo()).ToList(),
                room.Live);

            return new JoinRoomResponseCommand(response);
        }
        catch (Exception ex)
        {
            return new JoinRoomResponseCommand(ex.Message);
        }
    }

    private async Task<ServerCommand> HandleLeaveRoomAsync()
    {
        try
        {
            var room = User.Room ?? throw new Exception("No room");
            Logger.Info($"User {User.Id} left room {room.Id}");

            if (await room.OnUserLeaveAsync(User))
            {
                Server.Rooms.TryRemove(room.Id.Value, out _);
            }

            return new LeaveRoomResponseCommand(true);
        }
        catch (Exception ex)
        {
            return new LeaveRoomResponseCommand(false, ex.Message);
        }
    }

    private async Task<ServerCommand> HandleLockRoomAsync(LockRoomCommand cmd)
    {
        try
        {
            var room = User.Room ?? throw new Exception("No room");
            room.CheckHost(User);

            Logger.Info($"User {User.Id} set room {room.Id} lock to {cmd.Lock}");

            room.Locked = cmd.Lock;
            await room.SendAsync(new LockRoomMessage(cmd.Lock));

            return new LockRoomResponseCommand(true);
        }
        catch (Exception ex)
        {
            return new LockRoomResponseCommand(false, ex.Message);
        }
    }

    private async Task<ServerCommand> HandleCycleRoomAsync(CycleRoomCommand cmd)
    {
        try
        {
            var room = User.Room ?? throw new Exception("No room");
            room.CheckHost(User);

            Logger.Info($"User {User.Id} set room {room.Id} cycle to {cmd.Cycle}");

            room.SetCycle(cmd.Cycle);
            await room.SendAsync(new CycleRoomMessage(cmd.Cycle));

            return new CycleRoomResponseCommand(true);
        }
        catch (Exception ex)
        {
            return new CycleRoomResponseCommand(false, ex.Message);
        }
    }

    private async Task<ServerCommand> HandleSelectChartAsync(SelectChartCommand cmd)
    {
        try
        {
            var room = User.Room ?? throw new Exception("No room");
            if (room.State is not InternalRoomState.SelectChart)
                throw new Exception("Invalid state");

            room.CheckCanSelectChart(User);

            Logger.Debug($"User {User.Id} in room {room.Id} selecting chart {cmd.Id}");

            // 使用共享 HttpClient，避免端口耗尽
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(HttpTimeoutSeconds));
            var response = await SharedHttpClient.GetAsync($"{PhiraHost}/chart/{cmd.Id}", cts.Token);
            response.EnsureSuccessStatusCode();

            var chart = await response.Content.ReadFromJsonAsync<ChartInfo>(cancellationToken: cts.Token);
            if (chart == null)
                throw new Exception("Failed to fetch chart");

            Logger.Debug($"Chart is {chart.Name} (ID: {chart.Id})");

            if (room.Cycle && room.CycleVotingMode)
            {
                // In Cycle mode with voting enabled, store the vote
                room.VoteChart(User, chart);
                // Also set as current chart so clients know a chart is selected
                room.Chart = chart;
                await room.SendAsync(new SelectChartMessage(User.Id, chart.Name, chart.Id));
                await room.OnStateChangeAsync();
                Logger.Debug($"User {User.Id} voted for chart {chart.Id} in Cycle voting mode");
            }
            else
            {
                // In normal mode or Cycle without voting, host directly sets the chart
                await room.SendAsync(new SelectChartMessage(User.Id, chart.Name, chart.Id));
                room.Chart = chart;
                await room.OnStateChangeAsync();
            }

            return new SelectChartResponseCommand(true);
        }
        catch (OperationCanceledException)
        {
            Logger.Warning($"Chart fetch timeout for user {User.Id}");
            return new SelectChartResponseCommand(false, "Chart fetch timeout");
        }
        catch (Exception ex)
        {
            return new SelectChartResponseCommand(false, ex.Message);
        }
    }

    private async Task<ServerCommand> HandleRequestStartAsync()
    {
        try
        {
            var room = User.Room ?? throw new Exception("No room");
            if (room.State is not InternalRoomState.SelectChart)
                throw new Exception("Invalid state");
            if (room.GetAllUsers().Count < 2)
                throw new Exception("If no one is looking for you to play, you can go out and relax.");

            room.CheckHost(User);

            // In Cycle mode with voting enabled, randomly select a chart from votes
            if (room.Cycle && room.CycleVotingMode)
            {
                var selectedChart = room.SelectRandomChartFromVotes();
                if (selectedChart == null)
                    throw new Exception("No chart selected");

                room.Chart = selectedChart;
                Logger.Info(
                    $"Room {room.Id} in Cycle voting mode randomly selected chart {selectedChart.Id} from {room.ChartVotes.Count} votes");

                // Revoke fake host status from all non-host users
                var users = room.GetUsers();
                foreach (var user in users)
                {
                    if (!room.IsHost(user))
                    {
                        await user.TrySendAsync(new ChangeHostCommand(false));
                    }
                }

                // Clear votes for next round
                room.ClearVotes();

                // Notify all users of the final selected chart
                await room.OnStateChangeAsync();
            }
            else
            {
                if (room.Chart == null)
                    throw new Exception("No chart selected");
            }

            Logger.Debug($"Room {room.Id} waiting for ready");

            room.ResetGameTime();
            await room.SendAsync(new GameStartMessage(User.Id));

            room.State = new InternalRoomState.WaitForReady { Started = new HashSet<int> { User.Id } };
            await room.OnStateChangeAsync();
            await room.CheckAllReadyAsync();

            return new RequestStartResponseCommand(true);
        }
        catch (Exception ex)
        {
            return new RequestStartResponseCommand(false, ex.Message);
        }
    }

    private async Task<ServerCommand> HandleReadyAsync()
    {
        try
        {
            var room = User.Room ?? throw new Exception("No room");

            if (room.State is InternalRoomState.WaitForReady waitState)
            {
                if (!waitState.Started.Add(User.Id))
                    throw new Exception("Already ready");

                await room.SendAsync(new ReadyMessage(User.Id));
                await room.CheckAllReadyAsync();
            }

            return new ReadyResponseCommand(true);
        }
        catch (Exception ex)
        {
            return new ReadyResponseCommand(false, ex.Message);
        }
    }

    private async Task<ServerCommand> HandleCancelReadyAsync()
    {
        try
        {
            var room = User.Room ?? throw new Exception("No room");

            if (room.State is InternalRoomState.WaitForReady waitState)
            {
                if (!waitState.Started.Remove(User.Id))
                    throw new Exception("Not ready");

                if (room.IsHost(User))
                {
                    await room.SendAsync(new CancelGameMessage(User.Id));
                    room.State = new InternalRoomState.SelectChart();
                    if (room.CycleVotingMode && room.Cycle)
                    {
                        room.Chart = null;
                        var users = room.GetUsers();
                        foreach (var user in users)
                        {
                            if (!room.IsHost(user))
                            {
                                user.TrySendAsync(new ChangeHostCommand(true)).Wait();
                            }
                        }
                    }

                    await room.OnStateChangeAsync();
                }
                else
                {
                    await room.SendAsync(new CancelReadyMessage(User.Id));
                }
            }

            return new CancelReadyResponseCommand(true);
        }
        catch (Exception ex)
        {
            return new CancelReadyResponseCommand(false, ex.Message);
        }
    }

    private async Task<ServerCommand> HandlePlayedAsync(PlayedCommand cmd)
    {
        try
        {
            var room = User.Room ?? throw new Exception("No room");

            // 使用共享 HttpClient，避免端口耗尽
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(HttpTimeoutSeconds));
            var response = await SharedHttpClient.GetAsync($"{PhiraHost}/record/{cmd.Id}", cts.Token);
            response.EnsureSuccessStatusCode();

            var record = await response.Content.ReadFromJsonAsync<RecordInfo>(cancellationToken: cts.Token);
            if (record == null || record.Player != User.Id)
                throw new Exception("Invalid record");

            Logger.Debug(
                $"Room {room.Id} user {User.Id} played: Score={record.Score}, Accuracy={record.Accuracy}, FC={record.FullCombo}");

            await room.SendAsync(new PlayedMessage(
                User.Id, record.Score, record.Accuracy, record.FullCombo));

            if (room.State is InternalRoomState.Playing playingState)
            {
                if (playingState.Aborted.Contains(User.Id))
                    throw new Exception("Aborted");

                if (playingState.Results.ContainsKey(User.Id))
                    throw new Exception("Already uploaded");

                playingState.Results[User.Id] = record;
                await room.CheckAllReadyAsync();
            }

            return new PlayedResponseCommand(true);
        }
        catch (OperationCanceledException)
        {
            Logger.Warning($"Record fetch timeout for user {User.Id}");
            return new PlayedResponseCommand(false, "Record fetch timeout");
        }
        catch (Exception ex)
        {
            return new PlayedResponseCommand(false, ex.Message);
        }
    }

    private async Task<ServerCommand> HandleAbortAsync()
    {
        try
        {
            var room = User.Room ?? throw new Exception("No room");

            if (room.State is InternalRoomState.Playing playingState)
            {
                if (playingState.Results.ContainsKey(User.Id))
                    throw new Exception("Already uploaded");

                if (!playingState.Aborted.Add(User.Id))
                    throw new Exception("Already aborted");

                await room.SendAsync(new AbortMessage(User.Id));
                await room.CheckAllReadyAsync();
            }

            return new AbortResponseCommand(true);
        }
        catch (Exception ex)
        {
            return new AbortResponseCommand(false, ex.Message);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
        _cts.Cancel();
        Stream?.Dispose();

        try
        {
            _monitorTask.Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            // Ignore
        }

        _cts.Dispose();
    }

    ~Session()
    {
        Dispose();
    }
}