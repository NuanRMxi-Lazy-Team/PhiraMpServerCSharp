namespace PhiraMp.Core;

public class PlayedCommand : ClientCommand
{
    public override byte TypeTag => 14;
    public int Id { get; }

    public PlayedCommand(int id)
    {
        Id = id;
    }
}
