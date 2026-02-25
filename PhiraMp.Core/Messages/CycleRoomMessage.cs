namespace PhiraMp.Core;

public class CycleRoomMessage : Message
{
    public override byte TypeTag => 15;
    public bool Cycle { get; set; }

    public CycleRoomMessage(bool cycle)
    {
        Cycle = cycle;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteBool(Cycle);
    }
}
