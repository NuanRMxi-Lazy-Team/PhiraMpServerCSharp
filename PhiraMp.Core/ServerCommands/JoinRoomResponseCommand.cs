namespace PhiraMp.Core;

public class JoinRoomResponseCommand : ServerCommand
{
    public override byte TypeTag => 9;
    public bool Success { get; set; }
    public JoinRoomResponse? Response { get; set; }
    public string? Error { get; set; }

    public JoinRoomResponseCommand(JoinRoomResponse response)
    {
        Success = true;
        Response = response;
    }

    public JoinRoomResponseCommand(string error)
    {
        Success = false;
        Error = error;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteBool(Success);
        if (Success)
            Response!.WriteBinary(writer);
        else
            writer.WriteString(Error!);
    }
}
