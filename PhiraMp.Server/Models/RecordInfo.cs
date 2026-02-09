namespace PhiraMp.Server.Models;

public record RecordInfo
{
    public int Id { get; set; }
    public int Player { get; set; }
    public int Score { get; set; }
    public int Perfect { get; set; }
    public int Good { get; set; }
    public int Bad { get; set; }
    public int Miss { get; set; }
    public int MaxCombo { get; set; }
    public float Accuracy { get; set; }
    public bool FullCombo { get; set; }
    public float Std { get; set; }
    public float StdScore { get; set; }
}