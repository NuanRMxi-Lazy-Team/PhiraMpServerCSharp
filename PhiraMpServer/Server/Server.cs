using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using PhiraMpServer.Common;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PhiraMpServer.Server;



public class ServerState
{
    public ServerConfig Config { get; }
    public ConcurrentDictionary<Guid, Session> Sessions { get; } = new();
    public ConcurrentDictionary<int, User> Users { get; } = new();
    public ConcurrentDictionary<string, Room> Rooms { get; } = new();
    public Channel<Guid> LostConnectionChannel { get; }
    public ExternalInterface.Server? ExternalInterfaceServer { get; set; }

    public ServerState(ServerConfig config)
    {
        Config = config;
        LostConnectionChannel = Channel.CreateUnbounded<Guid>();
    }

    public async Task LostConnectionAsync(Guid sessionId)
    {
        await LostConnectionChannel.Writer.WriteAsync(sessionId);
    }
}

public class PhiraMpServer : IDisposable
{
    private readonly ServerState _state;
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts;
    private readonly Task _lostConnectionTask;
    private readonly ExternalInterface.Server? _externalInterfaceServer;
    private bool _disposed;

    public PhiraMpServer(ServerConfig config, ServerState serverState, ExternalInterface.Server? externalInterfaceServer = null)
    {
        _cts = new CancellationTokenSource();
        _state = serverState;

        var bindAddress = IPAddress.Parse(config.BindIp);
        _listener = new TcpListener(bindAddress, config.Port);

        // Enable dual-stack mode for IPv6
        if (bindAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            _listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }

        _lostConnectionTask = Task.Run(ProcessLostConnections);

        _externalInterfaceServer = externalInterfaceServer;
        _state.ExternalInterfaceServer = externalInterfaceServer;
    }

    public async Task StartAsync()
    {
        _listener.Start();
        Logger.Info($"Server listening on port {((IPEndPoint)_listener.LocalEndpoint).Port}");

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = Task.Run(() => HandleClientAsync(client), _cts.Token);
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
            if (_state.Sessions.Count >= _state.Config.ServerMaxPlayers)
            {
                Logger.Warning($"Server is full, rejecting connection from {endpoint}");
                var tempSession = await Session.CreateAsync(sessionId, client, _state);
                await tempSession.Stream.SendAsync(new AuthenticateResponseCommand("Server is full"));
                await Task.Delay(10);
                tempSession.Dispose();
                return;
            }
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