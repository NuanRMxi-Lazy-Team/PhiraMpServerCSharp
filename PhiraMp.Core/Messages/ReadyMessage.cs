namespace PhiraMp.Core;

public class ReadyMessage : Message
{
    public override byte TypeTag => 7;
    public int User { get; set; }

    public ReadyMessage(int user)
    {
        User = user;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteInt32(User);
    }
}
