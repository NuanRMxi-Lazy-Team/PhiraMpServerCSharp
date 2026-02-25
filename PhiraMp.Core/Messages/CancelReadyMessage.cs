namespace PhiraMp.Core;

public class CancelReadyMessage : Message
{
    public override byte TypeTag => 8;
    public int User { get; set; }

    public CancelReadyMessage(int user)
    {
        User = user;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteInt32(User);
    }
}
