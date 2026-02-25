namespace PhiraMp.Core;

public class SelectChartCommand : ClientCommand
{
    public override byte TypeTag => 10;
    public int Id { get; }

    public SelectChartCommand(int id)
    {
        Id = id;
    }
}
