namespace PhiraMp.Core;

public abstract class Message : IBinaryData
{
    public abstract byte TypeTag { get; }
    public abstract void WriteBinary(BinaryWriter writer);

    public static Message ReadBinary(BinaryReader reader)
    {
        var tag = reader.ReadByte();
        return tag switch
        {
            0 => new ChatMessage(reader.ReadInt32(), reader.ReadString()),
            1 => new CreateRoomMessage(reader.ReadInt32()),
            2 => new JoinRoomMessage(reader.ReadInt32(), reader.ReadString()),
            3 => new LeaveRoomMessage(reader.ReadInt32(), reader.ReadString()),
            4 => new NewHostMessage(reader.ReadInt32()),
            5 => new SelectChartMessage(reader.ReadInt32(), reader.ReadString(), reader.ReadInt32()),
            6 => new GameStartMessage(reader.ReadInt32()),
            7 => new ReadyMessage(reader.ReadInt32()),
            8 => new CancelReadyMessage(reader.ReadInt32()),
            9 => new CancelGameMessage(reader.ReadInt32()),
            10 => new StartPlayingMessage(),
            11 => new PlayedMessage(reader.ReadInt32(), reader.ReadInt32(), reader.ReadSingle(), reader.ReadBool()),
            12 => new GameEndMessage(),
            13 => new AbortMessage(reader.ReadInt32()),
            14 => new LockRoomMessage(reader.ReadBool()),
            15 => new CycleRoomMessage(reader.ReadBool()),
            _ => throw new InvalidOperationException($"Invalid message tag: {tag}")
        };
    }
}
