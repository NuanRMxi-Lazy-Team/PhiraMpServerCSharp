namespace PhiraMp.Core;

public class TouchesCommand : ClientCommand
{
    public override byte TypeTag => 3;
    public List<TouchFrame> Frames { get; }

    public TouchesCommand(List<TouchFrame> frames)
    {
        Frames = frames;
    }
}
