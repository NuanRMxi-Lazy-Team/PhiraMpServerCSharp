using PhiraMpServer.ExternalInterface.Model;
using ProtoBuf;

namespace PhiraMpServer.ExternalInterface.Common;

[ProtoContract]
[ProtoInclude(100, typeof(GetAllRoomResponse))]
[ProtoInclude(101, typeof(SetServerRoomMaxPlayersResponse))]
[ProtoInclude(102, typeof(AuthenticateResponse))]
[ProtoInclude(103, typeof(GetServerStatusResponse))]
[ProtoInclude(104, typeof(GetAllPlayerResponse))]
[ProtoInclude(105, typeof(GetRoomResponse))]
[ProtoInclude(999, typeof(UnknowCommandResponse))]
public abstract class CommandResponse
{
    [ProtoMember(1)] public string Token { get; set; } = string.Empty;
}

[ProtoContract]
public class GetAllRoomResponse : CommandResponse
{
    [ProtoMember(2)] public string[] RoomIdList { get; set; } = [];
}

[ProtoContract]
public class GetRoomResponse : CommandResponse
{
    [ProtoMember(2)] public RoomRecord? RoomInfo { get; set; }
}

[ProtoContract]
public class SetServerRoomMaxPlayersResponse : CommandResponse
{
    [ProtoMember(2)] public bool IsSuccess { get; set; }
    [ProtoMember(3)] public string Message { get; set; } = string.Empty;
}

[ProtoContract]
public class AuthenticateResponse : CommandResponse
{
    [ProtoMember(2)] public bool IsSuccess { get; set; }
    [ProtoMember(3)] public string Message { get; set; } = string.Empty;
}

[ProtoContract]
public class UnknowCommandResponse : CommandResponse
{
    [ProtoMember(2)] public string Message { get; set; } = "Unknown Command";
}

[ProtoContract]
public class GetServerStatusResponse : CommandResponse
{
    [ProtoMember(2)] public TimeSpan Uptime { get; set; }
    [ProtoMember(3)] public int MaxPlayers { get; set; }
    [ProtoMember(4)] public int CurrentPlayers { get; set; }
    [ProtoMember(5)] public string ExternalAddress { get; set; } = string.Empty;
}

[ProtoContract]
public class GetAllPlayerResponse : CommandResponse
{
    [ProtoMember(2)] public int[] PlayerList { get; set; } = [];
}