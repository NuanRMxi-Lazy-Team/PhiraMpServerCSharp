namespace PhiraMp.Core;

public class AbortMessage : Message
{
    public override byte TypeTag => 13;
    public int User { get; set; }

    public AbortMessage(int user)
    {
        User = user;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteInt32(User);
    }
}
