namespace PhiraMp.Core;

public class ChangeStateCommand : ServerCommand
{
    public override byte TypeTag => 6;
    public RoomStateData State { get; set; }

    public ChangeStateCommand(RoomStateData state)
    {
        State = state;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        State.WriteBinary(writer);
    }
}
