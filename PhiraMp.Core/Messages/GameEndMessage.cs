namespace PhiraMp.Core;

public class GameEndMessage : Message
{
    public override byte TypeTag => 12;

    public GameEndMessage()
    {
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
    }
}
