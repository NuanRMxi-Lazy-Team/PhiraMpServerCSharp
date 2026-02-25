namespace PhiraMp.Core;

public class ServerTouchesCommand : ServerCommand
{
    public override byte TypeTag => 3;
    public int Player { get; set; }
    public List<TouchFrame> Frames { get; set; }

    public ServerTouchesCommand(int player, List<TouchFrame> frames)
    {
        Player = player;
        Frames = frames;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteInt32(Player);
        writer.WriteArray(Frames, (w, f) => f.WriteBinary(w));
    }
}
