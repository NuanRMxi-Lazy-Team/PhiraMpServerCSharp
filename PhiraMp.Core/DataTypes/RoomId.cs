namespace PhiraMp.Core;

public class RoomId : IBinaryData
{
    public string Value { get; }

    public RoomId(string value)
    {
        if (value.Length == 0 || value.Length > 20)
            throw new ArgumentException("Invalid room id length");

        foreach (var c in value)
        {
            if (c != '-' && c != '_' && !char.IsAsciiLetterOrDigit(c))
                throw new ArgumentException("Invalid room id character");
        }

        Value = value;
    }

    public static RoomId ReadBinary(BinaryReader reader)
    {
        var varchar = Varchar.ReadBinary(reader, 20);
        return new RoomId(varchar.Value);
    }

    public void WriteBinary(BinaryWriter writer)
    {
        new Varchar(Value, 20).WriteBinary(writer);
    }

    public override string ToString() => Value;
}
