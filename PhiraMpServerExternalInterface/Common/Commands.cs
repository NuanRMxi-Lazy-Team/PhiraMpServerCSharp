using ProtoBuf;

namespace PhiraMpServer.ExternalInterface.Common;

[ProtoContract]
[ProtoInclude(100, typeof(GetAllRoomCommand))]
[ProtoInclude(101, typeof(SetRoomMaxPlayersCommand))]
public abstract class Command
{
    [ProtoMember(1)]
    public string Token { get; set; } = string.Empty;
}

[ProtoContract]
public class GetAllRoomCommand : Command;

[ProtoContract]
public class SetRoomMaxPlayersCommand : Command
{

    [ProtoMember(2)]
    public string RoomId { get; set; } = string.Empty;

    [ProtoMember(3)]
    public int MaxPlayers { get; set; }
}