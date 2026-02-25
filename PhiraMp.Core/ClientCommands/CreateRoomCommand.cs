namespace PhiraMp.Core;

public class CreateRoomCommand : ClientCommand
{
    public override byte TypeTag => 5;
    public RoomId Id { get; }

    public CreateRoomCommand(RoomId id)
    {
        Id = id;
    }
}
