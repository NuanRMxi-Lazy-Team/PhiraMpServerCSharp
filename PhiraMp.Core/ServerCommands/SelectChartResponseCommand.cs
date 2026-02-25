namespace PhiraMp.Core;

public class SelectChartResponseCommand : ServerCommand
{
    public override byte TypeTag => 14;
    public bool Success { get; set; }
    public string? Error { get; set; }

    public SelectChartResponseCommand(bool success, string? error = null)
    {
        Success = success;
        Error = error;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteBool(Success);
        if (!Success) writer.WriteString(Error!);
    }
}
