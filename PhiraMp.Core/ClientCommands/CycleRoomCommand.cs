namespace PhiraMp.Core;

public class CycleRoomCommand : ClientCommand
{
    public override byte TypeTag => 9;
    public bool Cycle { get; }

    public CycleRoomCommand(bool cycle)
    {
        Cycle = cycle;
    }
}
