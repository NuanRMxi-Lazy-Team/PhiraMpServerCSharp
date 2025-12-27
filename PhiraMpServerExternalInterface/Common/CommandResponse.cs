using PhiraMpServer.ExternalInterface.Module;
using ProtoBuf;

namespace PhiraMpServer.ExternalInterface.Common;

[ProtoContract] 
[ProtoInclude(100, typeof(GetAllRoomResponse))]
[ProtoInclude(101, typeof(SetRoomMaxPlayersResponse))]
[ProtoInclude(999, typeof(UnknowCommandResponse))]
public abstract class CommandResponse
{
    [ProtoMember(1)]
    public string Token { get; set; } = string.Empty;
}

[ProtoContract] 
public class GetAllRoomResponse : CommandResponse
{
    [ProtoMember(2)]
    public List<RoomRecord> RoomIdList { get; set; } = [];
}

[ProtoContract]
public class SetRoomMaxPlayersResponse : CommandResponse
{
    [ProtoMember(2)]
    public bool IsSuccess { get; set; }
}

public class UnknowCommandResponse : CommandResponse
{
    [ProtoMember(2)]
    public string Message { get; set; } = "Unknown Command";
}