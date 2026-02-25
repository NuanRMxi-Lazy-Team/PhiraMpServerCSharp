namespace PhiraMp.Core;

public class MessageCommand : ServerCommand
{
    public override byte TypeTag => 5;
    public Message Message { get; set; }

    public MessageCommand(Message message)
    {
        Message = message;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        Message.WriteBinary(writer);
    }
}
