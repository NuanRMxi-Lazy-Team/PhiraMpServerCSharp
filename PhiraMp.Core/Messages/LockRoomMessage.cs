namespace PhiraMp.Core;

public class LockRoomMessage : Message
{
    public override byte TypeTag => 14;
    public bool Lock { get; set; }

    public LockRoomMessage(bool lockState)
    {
        Lock = lockState;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteBool(Lock);
    }
}
