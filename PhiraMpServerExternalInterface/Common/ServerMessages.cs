using PhiraMpServer.ExternalInterface.Model;
using ProtoBuf;

namespace PhiraMpServer.ExternalInterface.Common;

[ProtoContract]
[ProtoInclude(100, typeof(RoomsChangedMessage))]
[ProtoInclude(101, typeof(RoomChangedMessage))]
public abstract class ServerMessages;

[ProtoContract]
public class RoomsChangedMessage : ServerMessages
{
    [ProtoMember(1)]
    public string[] RoomList { get; set; } = [];
}

[ProtoContract]
public class RoomChangedMessage : ServerMessages
{
    [ProtoMember(1)]
    public RoomRecord Room { get; set; } = new();
}