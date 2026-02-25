namespace PhiraMp.Core;

public class CompactPos : IBinaryData
{
    public Half X { get; set; }
    public Half Y { get; set; }

    public CompactPos(float x, float y)
    {
        X = Half.FromFloat(x);
        Y = Half.FromFloat(y);
    }

    public CompactPos(Half x, Half y)
    {
        X = x;
        Y = y;
    }

    public static CompactPos ReadBinary(BinaryReader reader)
    {
        var x = new Half(reader.ReadUInt16());
        var y = new Half(reader.ReadUInt16());
        return new CompactPos(x, y);
    }

    public void WriteBinary(BinaryWriter writer)
    {
        writer.WriteUInt16(X.ToBits());
        writer.WriteUInt16(Y.ToBits());
    }
}
