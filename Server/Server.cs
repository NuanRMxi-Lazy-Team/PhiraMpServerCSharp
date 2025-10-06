using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PhiraMpServer.Server;

public class ServerConfig
{
    public string BindIp { get; set; } = "::";
    public int Port { get; set; } = 12346;
    public int RoomMaxPlayers { get; set; } = 8;
    public List<int> Monitors { get; set; } = new() { 2 };

    public static ServerConfig Load(string path = "server_config.yml")
    {
        try
        {
            if (!File.Exists(path))
            {
                // Create default config file
                var defaultConfig = new ServerConfig();
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();
                var yamlnew = serializer.Serialize(defaultConfig);
                File.WriteAllText(path, yamlnew);
                return new ServerConfig();
            }
                

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var yaml = File.ReadAllText(path);
            return deserializer.Deserialize<ServerConfig>(yaml) ?? new ServerConfig();
        }
        catch
        {
            return new ServerConfig();
        }
    }
}

public class ServerState
{
    public ServerConfig Config { get; }
    public ConcurrentDictionary<Guid, Session> Sessions { get; } = new();
    public ConcurrentDictionary<int, User> Users { get; } = new();
    public ConcurrentDictionary<string, Room> Rooms { get; } = new();
    public Channel<Guid> LostConnectionChannel { get; }

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
    private readonly ILogger<PhiraMpServer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly CancellationTokenSource _cts;
    private readonly Task _lostConnectionTask;
    private bool _disposed;

    public PhiraMpServer(ILoggerFactory loggerFactory, ServerConfig? config = null)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<PhiraMpServer>();
        _cts = new CancellationTokenSource();

        config ??= ServerConfig.Load();
        _state = new ServerState(config);

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
        _listener.Start();
        _logger.LogInformation("Server listening on port {Port}", ((IPEndPoint)_listener.LocalEndpoint).Port);

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
                _logger.LogWarning(ex, "Failed to accept connection");
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        var endpoint = client.Client.RemoteEndPoint;
        var sessionId = Guid.NewGuid();

        try
        {
            var sessionLogger = _loggerFactory.CreateLogger<Session>();
            var session = await Session.CreateAsync(sessionId, client, _state, sessionLogger);

            _logger.LogInformation("Received connection from {Endpoint} ({SessionId}), version: {Version}",
                endpoint, sessionId, session.Stream.Version);

            _state.Sessions[sessionId] = session;

            // Session will run until disconnected
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle client {Endpoint}", endpoint);
            client.Dispose();
        }
    }

    private async Task ProcessLostConnections()
    {
        try
        {
            await foreach (var sessionId in _state.LostConnectionChannel.Reader.ReadAllAsync(_cts.Token))
            {
                _logger.LogWarning("Lost connection with {SessionId}", sessionId);

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
                            await user.DangleAsync(_logger);
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
