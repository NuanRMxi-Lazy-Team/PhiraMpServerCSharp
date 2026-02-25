namespace PhiraMp.Core;

public class RoomStateData : IBinaryData
{
    public RoomState State { get; set; }
    public int? ChartId { get; set; }

    public RoomStateData(RoomState state, int? chartId)
    {
        State = state;
        ChartId = chartId;
    }

    public static RoomStateData ReadBinary(BinaryReader reader)
    {
        var stateTag = reader.ReadByte();
        switch (stateTag)
        {
            case 0: // SelectChart
            {
                var hasChart = reader.ReadBool();
                var chartId = hasChart ? reader.ReadInt32() : (int?)null;
                return new RoomStateData(RoomState.SelectChart, chartId);
            }
            case 1: // WaitingForReady
                return new RoomStateData(RoomState.WaitingForReady, null);
            case 2: // Playing
                return new RoomStateData(RoomState.Playing, null);
            default:
                throw new InvalidOperationException($"Invalid room state: {stateTag}");
        }
    }

    public void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte((byte)State);
        if (State == RoomState.SelectChart)
        {
            writer.WriteBool(ChartId.HasValue);
            if (ChartId.HasValue)
                writer.WriteInt32(ChartId.Value);
        }
    }
}
