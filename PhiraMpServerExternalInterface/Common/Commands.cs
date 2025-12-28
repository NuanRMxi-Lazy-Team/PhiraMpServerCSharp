using ProtoBuf;

namespace PhiraMpServer.ExternalInterface.Common;

[ProtoContract]
[ProtoInclude(100, typeof(GetAllRoomCommand))]
[ProtoInclude(101, typeof(SetServerRoomMaxPlayersCommand))]
[ProtoInclude(102, typeof(AuthenticateCommand))]
[ProtoInclude(103, typeof(GetServerStatusCommand))]
[ProtoInclude(104, typeof(GetAllPlayerCommand))]
[ProtoInclude(105, typeof(GetRoomCommand))]
public abstract class Command
{
    [ProtoMember(1)]
    public string Token { get; set; } = string.Empty;
}

[ProtoContract]
public class AuthenticateCommand : Command
{
    [ProtoMember(2)]
    public string TokenSha256 { get; set; } = string.Empty;
}

[ProtoContract]
public class GetAllRoomCommand : Command;

[ProtoContract]
public class GetRoomCommand : Command
{
    [ProtoMember(2)]
    public string RoomId { get; set; } = string.Empty;
}

[ProtoContract]
public class SetServerRoomMaxPlayersCommand : Command
{
    [ProtoMember(2)]
    public int MaxPlayers { get; set; }
}

[ProtoContract]
public class GetServerStatusCommand : Command;

[ProtoContract]
public class GetAllPlayerCommand : Command;