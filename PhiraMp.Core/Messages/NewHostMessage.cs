namespace PhiraMp.Core;

public class NewHostMessage : Message
{
    public override byte TypeTag => 4;
    public int User { get; set; }

    public NewHostMessage(int user)
    {
        User = user;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteInt32(User);
    }
}
