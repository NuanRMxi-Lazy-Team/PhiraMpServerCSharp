namespace PhiraMp.Core;

public class PlayedMessage : Message
{
    public override byte TypeTag => 11;
    public int User { get; set; }
    public int Score { get; set; }
    public float Accuracy { get; set; }
    public bool FullCombo { get; set; }

    public PlayedMessage(int user, int score, float accuracy, bool fullCombo)
    {
        User = user;
        Score = score;
        Accuracy = accuracy;
        FullCombo = fullCombo;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteInt32(User);
        writer.WriteInt32(Score);
        writer.WriteSingle(Accuracy);
        writer.WriteBool(FullCombo);
    }
}
