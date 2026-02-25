namespace PhiraMp.Core;

public class GameStartMessage : Message
{
    public override byte TypeTag => 6;
    public int User { get; set; }

    public GameStartMessage(int user)
    {
        User = user;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteInt32(User);
    }
}
