namespace PhiraMp.Core;

public class LockRoomCommand : ClientCommand
{
    public override byte TypeTag => 8;
    public bool Lock { get; }

    public LockRoomCommand(bool lockState)
    {
        Lock = lockState;
    }
}
