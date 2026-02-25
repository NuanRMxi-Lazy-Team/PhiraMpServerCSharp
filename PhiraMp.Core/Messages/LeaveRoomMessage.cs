namespace PhiraMp.Core;

public class LeaveRoomMessage : Message
{
    public override byte TypeTag => 3;
    public int User { get; set; }
    public string Name { get; set; }

    public LeaveRoomMessage(int user, string name)
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
