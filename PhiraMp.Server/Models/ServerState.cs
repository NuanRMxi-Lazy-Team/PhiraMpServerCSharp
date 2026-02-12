﻿using System.Collections.Concurrent;
using System.Threading.Channels;
using PhiraMp.Server.Console;
using PhiraMp.Server.Plugins;

namespace PhiraMp.Server.Models;

public class ServerState
{
    public ServerConfig Config { get; }
    public ConcurrentDictionary<Guid, Session> Sessions { get; } = new();
    public ConcurrentDictionary<int, User> Users { get; } = new();
    public ConcurrentDictionary<string, Room> Rooms { get; } = new();
    public Channel<Guid> LostConnectionChannel { get; }
    public PluginManager? PluginManager { get; set; }
    public ConsoleCommandSystem? ConsoleCommandSystem { get; set; }

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