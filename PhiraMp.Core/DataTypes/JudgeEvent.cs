namespace PhiraMp.Core;

public class JudgeEvent : IBinaryData
{
    public float Time { get; set; }
    public uint LineId { get; set; }
    public uint NoteId { get; set; }
    public Judgement Judgement { get; set; }

    public JudgeEvent(float time, uint lineId, uint noteId, Judgement judgement)
    {
        Time = time;
        LineId = lineId;
        NoteId = noteId;
        Judgement = judgement;
    }

    public static JudgeEvent ReadBinary(BinaryReader reader)
    {
        var time = reader.ReadSingle();
        var lineId = reader.ReadUInt32();
        var noteId = reader.ReadUInt32();
        var judgement = (Judgement)reader.ReadByte();
        return new JudgeEvent(time, lineId, noteId, judgement);
    }

    public void WriteBinary(BinaryWriter writer)
    {
        writer.WriteSingle(Time);
        writer.WriteUInt32(LineId);
        writer.WriteUInt32(NoteId);
        writer.WriteByte((byte)Judgement);
    }
}
