namespace PhiraMp.Core;

public class UserInfo : IBinaryData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool Monitor { get; set; }

    public UserInfo(int id, string name, bool monitor)
    {
        Id = id;
        Name = name;
        Monitor = monitor;
    }

    public static UserInfo ReadBinary(BinaryReader reader)
    {
        var id = reader.ReadInt32();
        var name = reader.ReadString();
        var monitor = reader.ReadBool();
        return new UserInfo(id, name, monitor);
    }

    public void WriteBinary(BinaryWriter writer)
    {
        writer.WriteInt32(Id);
        writer.WriteString(Name);
        writer.WriteBool(Monitor);
    }
}
