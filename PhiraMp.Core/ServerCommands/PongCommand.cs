namespace PhiraMp.Core;

public class PongCommand : ServerCommand
{
    public override byte TypeTag => 0;

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
    }
}
