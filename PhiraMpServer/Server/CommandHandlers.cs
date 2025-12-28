using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using PhiraMpServer.ExternalInterface.Common;
using PhiraMpServer.ExternalInterface.Model;

namespace PhiraMpServer.Server;

public class GetAllRoomHandler : ICommandHandler<GetAllRoomCommand, GetAllRoomResponse>
{
    private readonly ServerState _serverState;

    public GetAllRoomHandler(ServerState serverState)
    {
        _serverState = serverState ?? throw new ArgumentNullException(nameof(serverState));
    }

    public Task<GetAllRoomResponse> HandleAsync(GetAllRoomCommand command)
    {
        return Task.FromResult(new GetAllRoomResponse
        {
            Token = command.Token,
            RoomIdList = _serverState.Rooms.Select(room => room.Key).ToArray(),
        });
    }
}

public class GetRoomHandler : ICommandHandler<GetRoomCommand, GetRoomResponse>
{
    private readonly ServerState _serverState;

    public GetRoomHandler(ServerState serverState)
    {
        _serverState = serverState ?? throw new ArgumentNullException(nameof(serverState));
    }

    public Task<GetRoomResponse> HandleAsync(GetRoomCommand command)
    {
        var room = _serverState.Rooms.GetValueOrDefault(command.RoomId);
        if (room == null)
        {
            return Task.FromResult(new GetRoomResponse
            {
                Token = command.Token,
                RoomInfo = null
            });
        }
        else
        {
            return Task.FromResult(new GetRoomResponse
                {
                    Token = command.Token,
                    RoomInfo = new RoomRecord
                    {
                        RoomId = room.Id.Value,
                        Players = room.GetUsers().Select(u => u.Id).ToArray(),
                        Monitors = room.GetMonitors().Select(u => u.Id).ToArray(),
                        Host = room.Host.Id,
                        IsLocked = room.Locked,
                        State = room.State switch
                        {
                            InternalRoomState.SelectChart => RoomState.SelectChart,
                            InternalRoomState.WaitForReady => RoomState.WaitingForReady,
                            InternalRoomState.Playing => RoomState.Playing,
                            _ => RoomState.SelectChart
                        },
                        Type = room is { Cycle: true, CycleVotingMode: true } ? RoomType.Voting :
                            room.Cycle ? RoomType.Cycle : RoomType.Normal,
                        SelectedCharts = room.State is InternalRoomState.Playing
                            ? [room.Chart!.Id]
                            : room is { Cycle: true, CycleVotingMode: true }
                                ? room.ChartVotes.Values.Select(c => c.Id).ToArray()
                                : room.Chart == null
                                    ? []
                                    : [room.Chart.Id],
                        ReadyInfo = room.GetUserReadyStates()
                    }
                }
            );
        }
    }
}

public class
    SetServerRoomMaxPlayersHandler : ICommandHandler<SetServerRoomMaxPlayersCommand, SetServerRoomMaxPlayersResponse>
{
    private readonly ServerState _serverState;

    public SetServerRoomMaxPlayersHandler(ServerState serverState)
    {
        _serverState = serverState ?? throw new ArgumentNullException(nameof(serverState));
    }

    public Task<SetServerRoomMaxPlayersResponse> HandleAsync(SetServerRoomMaxPlayersCommand command)
    {
        _serverState.Config.RoomMaxPlayers = command.MaxPlayers;
        return Task.FromResult(new SetServerRoomMaxPlayersResponse
        {
            Token = command.Token,
            IsSuccess = true,
            Message = "Room max players updated successfully."
        });
    }
}

public class AuthenticateHandler : ICommandHandler<AuthenticateCommand, AuthenticateResponse>
{
    private readonly ServerState _serverState;

    public AuthenticateHandler(ServerState serverState)
    {
        _serverState = serverState ?? throw new ArgumentNullException(nameof(serverState));
    }

    public Task<AuthenticateResponse> HandleAsync(AuthenticateCommand command)
    {
        var bytes = Encoding.UTF8.GetBytes(_serverState.Config.ExternalInterfaceToken);
        var hash = SHA256.HashData(bytes);
        var tokenHash = Convert.ToHexString(hash).ToLowerInvariant();

        if (command.TokenSha256 == tokenHash)
        {
            return Task.FromResult(new AuthenticateResponse
            {
                Token = command.Token,
                IsSuccess = true,
                Message = "Authentication successful."
            });
        }
        else
        {
            return Task.FromResult(new AuthenticateResponse
            {
                Token = command.Token,
                IsSuccess = false,
                Message = "Authentication failed. Invalid Token."
            });
        }
    }
}

public class GetServerStatusHandler : ICommandHandler<GetServerStatusCommand, GetServerStatusResponse>
{
    private readonly ServerState _serverState;
    private readonly DateTime _startTime;

    public GetServerStatusHandler(ServerState serverState, DateTime startTime)
    {
        _serverState = serverState ?? throw new ArgumentNullException(nameof(serverState));
        _startTime = startTime;
    }

    public Task<GetServerStatusResponse> HandleAsync(GetServerStatusCommand command)
    {
        return Task.FromResult(new GetServerStatusResponse
        {
            Token = command.Token,
            Uptime = DateTime.Now - _startTime,
            MaxPlayers = _serverState.Config.ServerMaxPlayers,
            CurrentPlayers = _serverState.Sessions.Count,
            ExternalAddress = _serverState.Config.ExternalAddress
        });
    }
}

public class GetAllPlayersHandler : ICommandHandler<GetAllPlayerCommand, GetAllPlayerResponse>
{
    private readonly ServerState _serverState;

    public GetAllPlayersHandler(ServerState serverState)
    {
        _serverState = serverState ?? throw new ArgumentNullException(nameof(serverState));
    }

    public Task<GetAllPlayerResponse> HandleAsync(GetAllPlayerCommand command)
    {
        return Task.FromResult(new GetAllPlayerResponse
        {
            Token = command.Token,
            PlayerList = _serverState.Sessions.Values.Select(s => s.User.Id).ToArray()
        });
    }
}