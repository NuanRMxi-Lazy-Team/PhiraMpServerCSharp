using PhiraMp.Core;
namespace PhiraMp.Server.Models;

public abstract record InternalRoomState
{
    public record SelectChart : InternalRoomState;

    public record WaitForReady : InternalRoomState
    {
        public HashSet<int> Started { get; init; } = new();
    }

    public record Playing : InternalRoomState
    {
        public Dictionary<int, RecordInfo> Results { get; init; } = new();
        public HashSet<int> Aborted { get; init; } = new();
    }

    public RoomStateData ToClient(int? chartId)
    {
        return this switch
        {
            SelectChart => new RoomStateData(RoomState.SelectChart, chartId),
            WaitForReady => new RoomStateData(RoomState.WaitingForReady, null),
            Playing => new RoomStateData(RoomState.Playing, null),
            _ => throw new InvalidOperationException()
        };
    }
}