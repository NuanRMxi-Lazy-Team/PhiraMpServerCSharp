using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PhiraMpServer.ExternalInterface.Common;
using PhiraMpServer.ExternalInterface.Module;

namespace PhiraMpServer.Server;

public static class ExternalInterfaceHandler
{
    public static async Task<CommandResponse> CommandHandler(ServerState serverState, Command command)
    {
        return command switch
        {
            GetAllRoomCommand cmd => GetAllRoomHandler(serverState, cmd),
            SetRoomMaxPlayersCommand cmd => GetSetRoomMaxPlayersHandler(serverState, cmd),
            _ => new UnknowCommandResponse
            {
                Token = command.Token,
                Message = "The command type is not recognized."
            }
        };
    }

    private static GetAllRoomResponse GetAllRoomHandler(ServerState serverState, GetAllRoomCommand command)
    {
        var rspRoomList = serverState.Rooms.Select(room => new RoomRecord
            {
                RoomId = room.Key,
                Players = room.Value.GetAllUsers().Select(u => u.Id).ToArray(),
                Host = room.Value.Host.Id,
                IsLocked = room.Value.Locked,
                State = room.Value.State switch
                {
                    InternalRoomState.SelectChart => RoomState.SelectChart,
                    InternalRoomState.WaitForReady => RoomState.WaitingForReady,
                    InternalRoomState.Playing => RoomState.Playing,
                    _ => RoomState.SelectChart
                },
                Type = room.Value is { Cycle: true, CycleVotingMode: true } ? RoomType.Voting :
                    room.Value.Cycle ? RoomType.Cycle : RoomType.Normal
            })
            .ToList();

        return new GetAllRoomResponse
        {
            Token = command.Token,
            RoomIdList = rspRoomList
        };
    }

    private static SetRoomMaxPlayersResponse GetSetRoomMaxPlayersHandler(ServerState serverState,
        SetRoomMaxPlayersCommand command)
    {
        serverState.Config.RoomMaxPlayers = command.MaxPlayers;
        return new SetRoomMaxPlayersResponse { IsSuccess = true };
    }
}