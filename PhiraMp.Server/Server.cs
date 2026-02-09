using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
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
        // Load plugins before starting server
        await _state.PluginManager!.LoadAllPluginsAsync();
        _state.PluginManager.EnableHotReload();

        _listener.Start();
        Logger.Info($"Server listening on port {((IPEndPoint)_listener.LocalEndpoint).Port}");

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