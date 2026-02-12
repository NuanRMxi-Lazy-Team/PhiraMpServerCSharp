using System.Net;
using System.Net.Sockets;
using PhiraMp.Server.Console;
using PhiraMp.Server.Models;
using PhiraMp.Server.Plugins;

namespace PhiraMp.Server;

public class PhiraMpServer : IDisposable
{
    private readonly ServerState _state;
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts;
    private readonly Task _lostConnectionTask;
    private bool _disposed;

    public PhiraMpServer(ServerConfig? config = null)
    {
        _cts = new CancellationTokenSource();

        config ??= ServerConfig.Load();
        _state = new ServerState(config);

        // Initialize console command system
        _state.ConsoleCommandSystem = new ConsoleCommandSystem();

        // Initialize plugin manager
        _state.PluginManager = new PluginManager(_state);

        var bindAddress = IPAddress.Parse(config.BindIp);
        _listener = new TcpListener(bindAddress, config.Port);

        // Enable dual-stack mode for IPv6
        if (bindAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            _listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }

        _lostConnectionTask = Task.Run(ProcessLostConnections);
    }

    public async Task StartAsync()
    {
        // 加载插件（包含热重载功能）
        await _state.PluginManager!.LoadAllPluginsAsync();

        _listener.Start();
        Logger.Info($"Server listening on port {((IPEndPoint)_listener.LocalEndpoint).Port}");
        
        // 在启动命令系统之前输出提示信息
        Logger.Info("控制台命令系统已启动");
        Logger.Info("提示: 使用 TAB 键自动补全命令，输入 'help' 查看所有命令");

        // 插件加载完成后，启动控制台命令系统
        _state.ConsoleCommandSystem?.Start(_cts.Token);

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                // Fire and forget - handle client asynchronously without awaiting
                _ = HandleClientAsync(client);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Warning($"Failed to accept connection: {ex.Message}");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        var endpoint = client.Client.RemoteEndPoint;
        var sessionId = Guid.NewGuid();

        try
        {
            var session = await Session.CreateAsync(sessionId, client, _state);

            Logger.Info($"Received connection from {endpoint} ({sessionId}), version: {session.Stream.Version}");

            _state.Sessions[sessionId] = session;

            // Session will run until disconnected
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to handle client {endpoint}:");
            client.Dispose();
        }
    }

    private async Task ProcessLostConnections()
    {
        try
        {
            await foreach (var sessionId in _state.LostConnectionChannel.Reader.ReadAllAsync(_cts.Token))
            {
                Logger.Warning($"Lost connection with {sessionId}");

                if (_state.Sessions.TryRemove(sessionId, out var session))
                {
                    var user = session.User;
                    if (user != null)
                    {
                        // Check if this is still the current session
                        Session? currentSession = null;
                        user.SessionRef?.TryGetTarget(out currentSession);

                        if (currentSession == session)
                        {
                            await user.DangleAsync();
                        }
                    }

                    session.Dispose();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
    }

    public void Stop()
    {
        _cts.Cancel();
        _listener.Stop();
    }

    /// <summary>
    /// 获取服务器状态（供控制台命令使用）
    /// </summary>
    public ServerState GetState() => _state;

    /// <summary>
    /// 获取取消令牌源（供控制台命令使用）
    /// </summary>
    public CancellationTokenSource GetCancellationTokenSource() => _cts;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
        Stop();

        // Dispose plugin manager
        _state.PluginManager?.Dispose();

        foreach (var session in _state.Sessions.Values)
        {
            session.Dispose();
        }

        _state.Sessions.Clear();

        try
        {
            _lostConnectionTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // Ignore
        }

        _cts.Dispose();
    }
}