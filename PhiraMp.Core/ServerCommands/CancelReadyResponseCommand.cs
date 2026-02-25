namespace PhiraMp.Core;

public class CancelReadyResponseCommand : ServerCommand
{
    public override byte TypeTag => 17;
    public bool Success { get; set; }
    public string? Error { get; set; }

    public CancelReadyResponseCommand(bool success, string? error = null)
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
