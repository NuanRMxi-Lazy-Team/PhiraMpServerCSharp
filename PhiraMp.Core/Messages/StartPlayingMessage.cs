namespace PhiraMp.Core;

public class StartPlayingMessage : Message
{
    public override byte TypeTag => 10;

    public StartPlayingMessage()
    {
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
    }
}
