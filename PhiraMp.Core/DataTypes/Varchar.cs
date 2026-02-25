namespace PhiraMp.Core;

public class Varchar : IBinaryData
{
    public string Value { get; set; }
    public int MaxLength { get; }

    public Varchar(string value, int maxLength)
    {
        if (value.Length > maxLength)
            throw new ArgumentException($"String too long: {value.Length} > {maxLength}");
        Value = value;
        MaxLength = maxLength;
    }

    public static Varchar ReadBinary(BinaryReader reader, int maxLength)
    {
        var value = reader.ReadString();
        return new Varchar(value, maxLength);
    }

    public void WriteBinary(BinaryWriter writer)
    {
        writer.WriteString(Value);
    }

    public override string ToString() => Value;
}
