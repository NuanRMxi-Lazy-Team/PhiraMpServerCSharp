namespace PhiraMp.Core;

public class CreateRoomMessage : Message
{
    public override byte TypeTag => 1;
    public int User { get; set; }

    public CreateRoomMessage(int user)
    {
        User = user;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteInt32(User);
    }
}
