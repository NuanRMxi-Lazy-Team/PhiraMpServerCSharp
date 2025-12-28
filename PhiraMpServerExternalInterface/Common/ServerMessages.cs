using PhiraMpServer.ExternalInterface.Model;
using ProtoBuf;

namespace PhiraMpServer.ExternalInterface.Common;

[ProtoContract]
[ProtoInclude(100, typeof(RoomChangedMessage))]
public abstract class ServerMessages;

[ProtoContract]
public class RoomChangedMessage : ServerMessages
{
    [ProtoMember(1)]
    public List<RoomRecord> RoomList { get; set; } = [];
}