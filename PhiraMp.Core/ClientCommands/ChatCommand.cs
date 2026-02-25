namespace PhiraMp.Core;

public class ChatCommand : ClientCommand
{
    public override byte TypeTag => 2;
    public Varchar Message { get; }

    public ChatCommand(Varchar message)
    {
        Message = message;
    }
}
