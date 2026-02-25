namespace PhiraMp.Core;

public class JudgesCommand : ClientCommand
{
    public override byte TypeTag => 4;
    public List<JudgeEvent> Judges { get; }

    public JudgesCommand(List<JudgeEvent> judges)
    {
        Judges = judges;
    }
}
