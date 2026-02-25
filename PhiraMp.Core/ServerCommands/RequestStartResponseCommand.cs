namespace PhiraMp.Core;

public class RequestStartResponseCommand : ServerCommand
{
    public override byte TypeTag => 15;
    public bool Success { get; set; }
    public string? Error { get; set; }

    public RequestStartResponseCommand(bool success, string? error = null)
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
