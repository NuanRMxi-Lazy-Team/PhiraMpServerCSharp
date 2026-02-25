namespace PhiraMp.Core;

public class OnJoinRoomCommand : ServerCommand
{
    public override byte TypeTag => 10;
    public UserInfo User { get; set; }

    public OnJoinRoomCommand(UserInfo user)
    {
        User = user;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        User.WriteBinary(writer);
    }
}
