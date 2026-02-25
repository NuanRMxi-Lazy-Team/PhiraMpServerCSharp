namespace PhiraMp.Core;

public class ChangeHostCommand : ServerCommand
{
    public override byte TypeTag => 7;
    public bool IsHost { get; set; }

    public ChangeHostCommand(bool isHost)
    {
        IsHost = isHost;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteBool(IsHost);
    }
}
