namespace PhiraMp.Core;

public class ChatMessage : Message
{
    public override byte TypeTag => 0;
    public int User { get; set; }
    public string Content { get; set; }

    public ChatMessage(int user, string content)
    {
        User = user;
        Content = content;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteInt32(User);
        writer.WriteString(Content);
    }
}
