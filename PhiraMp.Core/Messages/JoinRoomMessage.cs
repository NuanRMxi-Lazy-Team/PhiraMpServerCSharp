namespace PhiraMp.Core;

public class JoinRoomMessage : Message
{
    public override byte TypeTag => 2;
    public int User { get; set; }
    public string Name { get; set; }

    public JoinRoomMessage(int user, string name)
    {
        User = user;
        Name = name;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteInt32(User);
        writer.WriteString(Name);
    }
}
