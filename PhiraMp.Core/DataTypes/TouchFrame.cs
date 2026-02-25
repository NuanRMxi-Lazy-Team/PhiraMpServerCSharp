namespace PhiraMp.Core;

public class TouchFrame : IBinaryData
{
    public float Time { get; set; }
    public List<(sbyte Id, CompactPos Pos)> Points { get; set; }

    public TouchFrame(float time, List<(sbyte, CompactPos)> points)
    {
        Time = time;
        Points = points;
    }

    public static TouchFrame ReadBinary(BinaryReader reader)
    {
        var time = reader.ReadSingle();
        var points = reader.ReadArray(r =>
        {
            var id = r.ReadSByte();
            var pos = CompactPos.ReadBinary(r);
            return (id, pos);
        });
        return new TouchFrame(time, points);
    }

    public void WriteBinary(BinaryWriter writer)
    {
        writer.WriteSingle(Time);
        writer.WriteArray(Points, (w, point) =>
        {
            w.WriteSByte(point.Id);
            point.Pos.WriteBinary(w);
        });
    }
}
