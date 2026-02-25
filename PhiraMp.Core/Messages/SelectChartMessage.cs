namespace PhiraMp.Core;

public class SelectChartMessage : Message
{
    public override byte TypeTag => 5;
    public int User { get; set; }
    public string Name { get; set; }
    public int Id { get; set; }

    public SelectChartMessage(int user, string name, int id)
    {
        User = user;
        Name = name;
        Id = id;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteInt32(User);
        writer.WriteString(Name);
        writer.WriteInt32(Id);
    }
}
