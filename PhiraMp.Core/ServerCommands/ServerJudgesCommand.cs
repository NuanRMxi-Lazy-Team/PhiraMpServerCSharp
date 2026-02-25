namespace PhiraMp.Core;

public class ServerJudgesCommand : ServerCommand
{
    public override byte TypeTag => 4;
    public int Player { get; set; }
    public List<JudgeEvent> Judges { get; set; }

    public ServerJudgesCommand(int player, List<JudgeEvent> judges)
    {
        Player = player;
        Judges = judges;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteInt32(Player);
        writer.WriteArray(Judges, (w, j) => j.WriteBinary(w));
    }
}
