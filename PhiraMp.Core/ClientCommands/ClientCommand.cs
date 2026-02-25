namespace PhiraMp.Core;

public abstract class ClientCommand
{
    public abstract byte TypeTag { get; }

    public static ClientCommand ReadBinary(BinaryReader reader)
    {
        var tag = reader.ReadByte();

        // Validate tag range (0-15 are valid client commands)
        if (tag > 15)
        {
            throw new InvalidOperationException($"Invalid client command tag: {tag} (valid range: 0-15)");
        }

        return tag switch
        {
            0 => new PingCommand(),
            1 => new AuthenticateCommand(Varchar.ReadBinary(reader, 32)),
            2 => new ChatCommand(Varchar.ReadBinary(reader, 200)),
            3 => new TouchesCommand(reader.ReadArray(TouchFrame.ReadBinary)),
            4 => new JudgesCommand(reader.ReadArray(JudgeEvent.ReadBinary)),
            5 => new CreateRoomCommand(RoomId.ReadBinary(reader)),
            6 => new JoinRoomCommand(RoomId.ReadBinary(reader), reader.ReadBool()),
            7 => new LeaveRoomCommand(),
            8 => new LockRoomCommand(reader.ReadBool()),
            9 => new CycleRoomCommand(reader.ReadBool()),
            10 => new SelectChartCommand(reader.ReadInt32()),
            11 => new RequestStartCommand(),
            12 => new ReadyCommand(),
            13 => new CancelReadyCommand(),
            14 => new PlayedCommand(reader.ReadInt32()),
            15 => new AbortCommand(),
            _ => throw new InvalidOperationException($"Invalid client command tag: {tag}")
        };
    }
}
