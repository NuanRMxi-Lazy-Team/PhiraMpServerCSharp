namespace PhiraMp.Core;

public class JoinRoomCommand : ClientCommand
{
    public override byte TypeTag => 6;
    public RoomId Id { get; }
    public bool Monitor { get; }

    public JoinRoomCommand(RoomId id, bool monitor)
    {
        Id = id;
        Monitor = monitor;
    }
}
