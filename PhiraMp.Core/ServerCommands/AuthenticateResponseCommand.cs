namespace PhiraMp.Core;

public class AuthenticateResponseCommand : ServerCommand
{
    public override byte TypeTag => 1;
    public bool Success { get; set; }
    public UserInfo? UserInfo { get; set; }
    public ClientRoomState? RoomState { get; set; }
    public string? Error { get; set; }

    public AuthenticateResponseCommand(UserInfo userInfo, ClientRoomState? roomState)
    {
        Success = true;
        UserInfo = userInfo;
        RoomState = roomState;
    }

    public AuthenticateResponseCommand(string error)
    {
        Success = false;
        Error = error;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteBool(Success);
        if (Success)
        {
            UserInfo!.WriteBinary(writer);
            writer.WriteBool(RoomState != null);
            RoomState?.WriteBinary(writer);
        }
        else
        {
            writer.WriteString(Error!);
        }
    }
}
