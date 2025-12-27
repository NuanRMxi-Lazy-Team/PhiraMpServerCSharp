using ProtoBuf;
namespace PhiraMpServer.ExternalInterface.Module;

[ProtoContract]
public record RoomRecord
{
    [ProtoMember(1)]
    public string RoomId { get; init; } = string.Empty;
    [ProtoMember(2)]
    public int Host { get; init; }
    [ProtoMember(3)]
    public int[] Players { get; init; } = [];
    [ProtoMember(4)]
    public bool IsLocked { get; init; }
    [ProtoMember(5)]
    public RoomState State { get; init; }
    [ProtoMember(6)]
    public RoomType Type { get; init; }
}

[ProtoContract]
public enum RoomState
{
    [ProtoEnum]
    SelectChart,
    [ProtoEnum]
    WaitingForReady,
    [ProtoEnum]
    Playing
}

[ProtoContract]
public enum RoomType
{
    [ProtoEnum]
    Normal,
    [ProtoEnum]
    Cycle,
    [ProtoEnum]
    Voting
}