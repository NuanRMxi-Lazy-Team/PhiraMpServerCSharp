namespace PhiraMp.Core;

public class CancelGameMessage : Message
{
    public override byte TypeTag => 9;
    public int User { get; set; }

    public CancelGameMessage(int user)
    {
        User = user;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteInt32(User);
    }
}
