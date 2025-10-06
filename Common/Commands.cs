using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace PhiraMpServer.Common;

// ============ Data Types ============

public readonly struct Half
{
    private readonly ushort _value;

    public Half(ushort bits) => _value = bits;

    public static Half FromFloat(float value) => new Half(BitConverter.HalfToUInt16Bits((System.Half)value));
    public float ToFloat() => (float)BitConverter.UInt16BitsToHalf(_value);
    public ushort ToBits() => _value;
}

public class CompactPos : IBinaryData
{
    public Half X { get; set; }
    public Half Y { get; set; }

    public CompactPos(float x, float y)
    {
        X = Half.FromFloat(x);
        Y = Half.FromFloat(y);
    }

    public CompactPos(Half x, Half y)
    {
        X = x;
        Y = y;
    }

    public static CompactPos ReadBinary(BinaryReader reader)
    {
        var x = new Half(reader.ReadUInt16());
        var y = new Half(reader.ReadUInt16());
        return new CompactPos(x, y);
    }

    public void WriteBinary(BinaryWriter writer)
    {
        writer.WriteUInt16(X.ToBits());
        writer.WriteUInt16(Y.ToBits());
    }
}

public class Varchar : IBinaryData
{
    public string Value { get; set; }
    public int MaxLength { get; }

    public Varchar(string value, int maxLength)
    {
        if (value.Length > maxLength)
            throw new ArgumentException($"String too long: {value.Length} > {maxLength}");
        Value = value;
        MaxLength = maxLength;
    }

    public static Varchar ReadBinary(BinaryReader reader, int maxLength)
    {
        var value = reader.ReadString();
        return new Varchar(value, maxLength);
    }

    public void WriteBinary(BinaryWriter writer)
    {
        writer.WriteString(Value);
    }

    public override string ToString() => Value;
}

public class RoomId : IBinaryData
{
    public string Value { get; }

    public RoomId(string value)
    {
        if (value.Length == 0 || value.Length > 20)
            throw new ArgumentException("Invalid room id length");

        foreach (var c in value)
        {
            if (c != '-' && c != '_' && !char.IsAsciiLetterOrDigit(c))
                throw new ArgumentException("Invalid room id character");
        }

        Value = value;
    }

    public static RoomId ReadBinary(BinaryReader reader)
    {
        var varchar = Varchar.ReadBinary(reader, 20);
        return new RoomId(varchar.Value);
    }

    public void WriteBinary(BinaryWriter writer)
    {
        new Varchar(Value, 20).WriteBinary(writer);
    }

    public override string ToString() => Value;
}

public class TouchFrame : IBinaryData
{
    public float Time { get; set; }
    public List<(sbyte Id, CompactPos Pos)> Points { get; set; }

    public TouchFrame(float time, List<(sbyte, CompactPos)> points)
    {
        Time = time;
        Points = points;
    }

    public static TouchFrame ReadBinary(BinaryReader reader)
    {
        var time = reader.ReadSingle();
        var points = reader.ReadArray(r =>
        {
            var id = r.ReadSByte();
            var pos = CompactPos.ReadBinary(r);
            return (id, pos);
        });
        return new TouchFrame(time, points);
    }

    public void WriteBinary(BinaryWriter writer)
    {
        writer.WriteSingle(Time);
        writer.WriteArray(Points, (w, point) =>
        {
            w.WriteSByte(point.Id);
            point.Pos.WriteBinary(w);
        });
    }
}

public enum Judgement : byte
{
    Perfect = 0,
    Good = 1,
    Bad = 2,
    Miss = 3,
    HoldPerfect = 4,
    HoldGood = 5
}

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

public class UserInfo : IBinaryData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool Monitor { get; set; }

    public UserInfo(int id, string name, bool monitor)
    {
        Id = id;
        Name = name;
        Monitor = monitor;
    }

    public static UserInfo ReadBinary(BinaryReader reader)
    {
        var id = reader.ReadInt32();
        var name = reader.ReadString();
        var monitor = reader.ReadBool();
        return new UserInfo(id, name, monitor);
    }

    public void WriteBinary(BinaryWriter writer)
    {
        writer.WriteInt32(Id);
        writer.WriteString(Name);
        writer.WriteBool(Monitor);
    }
}

public enum RoomState : byte
{
    SelectChart = 0,
    WaitingForReady = 1,
    Playing = 2
}

public class RoomStateData : IBinaryData
{
    public RoomState State { get; set; }
    public int? ChartId { get; set; }

    public RoomStateData(RoomState state, int? chartId)
    {
        State = state;
        ChartId = chartId;
    }

    public static RoomStateData ReadBinary(BinaryReader reader)
    {
        var stateTag = reader.ReadByte();
        switch (stateTag)
        {
            case 0: // SelectChart
            {
                var hasChart = reader.ReadBool();
                var chartId = hasChart ? reader.ReadInt32() : (int?)null;
                return new RoomStateData(RoomState.SelectChart, chartId);
            }
            case 1: // WaitingForReady
                return new RoomStateData(RoomState.WaitingForReady, null);
            case 2: // Playing
                return new RoomStateData(RoomState.Playing, null);
            default:
                throw new InvalidOperationException($"Invalid room state: {stateTag}");
        }
    }

    public void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte((byte)State);
        if (State == RoomState.SelectChart)
        {
            writer.WriteBool(ChartId.HasValue);
            if (ChartId.HasValue)
                writer.WriteInt32(ChartId.Value);
        }
    }
}

public class ClientRoomState : IBinaryData
{
    public RoomId Id { get; set; }
    public RoomStateData State { get; set; }
    public bool Live { get; set; }
    public bool Locked { get; set; }
    public bool Cycle { get; set; }
    public bool IsHost { get; set; }
    public bool IsReady { get; set; }
    public Dictionary<int, UserInfo> Users { get; set; }

    public ClientRoomState(RoomId id, RoomStateData state, bool live, bool locked, bool cycle, bool isHost, bool isReady, Dictionary<int, UserInfo> users)
    {
        Id = id;
        State = state;
        Live = live;
        Locked = locked;
        Cycle = cycle;
        IsHost = isHost;
        IsReady = isReady;
        Users = users;
    }

    public void WriteBinary(BinaryWriter writer)
    {
        Id.WriteBinary(writer);
        State.WriteBinary(writer);
        writer.WriteBool(Live);
        writer.WriteBool(Locked);
        writer.WriteBool(Cycle);
        writer.WriteBool(IsHost);
        writer.WriteBool(IsReady);
        writer.WriteULEB((ulong)Users.Count);
        foreach (var (id, user) in Users)
        {
            writer.WriteInt32(id);
            user.WriteBinary(writer);
        }
    }
}

public class JoinRoomResponse : IBinaryData
{
    public RoomStateData State { get; set; }
    public List<UserInfo> Users { get; set; }
    public bool Live { get; set; }

    public JoinRoomResponse(RoomStateData state, List<UserInfo> users, bool live)
    {
        State = state;
        Users = users;
        Live = live;
    }

    public void WriteBinary(BinaryWriter writer)
    {
        State.WriteBinary(writer);
        writer.WriteArray(Users, (w, u) => u.WriteBinary(w));
        writer.WriteBool(Live);
    }
}

// ============ Messages ============

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

public class ChatMessage : Message
{
    public override byte TypeTag => 0;
    public int User { get; set; }
    public string Content { get; set; }
    public ChatMessage(int user, string content) { User = user; Content = content; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteInt32(User); writer.WriteString(Content); }
}

public class CreateRoomMessage : Message
{
    public override byte TypeTag => 1;
    public int User { get; set; }
    public CreateRoomMessage(int user) { User = user; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteInt32(User); }
}

public class JoinRoomMessage : Message
{
    public override byte TypeTag => 2;
    public int User { get; set; }
    public string Name { get; set; }
    public JoinRoomMessage(int user, string name) { User = user; Name = name; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteInt32(User); writer.WriteString(Name); }
}

public class LeaveRoomMessage : Message
{
    public override byte TypeTag => 3;
    public int User { get; set; }
    public string Name { get; set; }
    public LeaveRoomMessage(int user, string name) { User = user; Name = name; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteInt32(User); writer.WriteString(Name); }
}

public class NewHostMessage : Message
{
    public override byte TypeTag => 4;
    public int User { get; set; }
    public NewHostMessage(int user) { User = user; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteInt32(User); }
}

public class SelectChartMessage : Message
{
    public override byte TypeTag => 5;
    public int User { get; set; }
    public string Name { get; set; }
    public int Id { get; set; }
    public SelectChartMessage(int user, string name, int id) { User = user; Name = name; Id = id; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteInt32(User); writer.WriteString(Name); writer.WriteInt32(Id); }
}

public class GameStartMessage : Message
{
    public override byte TypeTag => 6;
    public int User { get; set; }
    public GameStartMessage(int user) { User = user; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteInt32(User); }
}

public class ReadyMessage : Message
{
    public override byte TypeTag => 7;
    public int User { get; set; }
    public ReadyMessage(int user) { User = user; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteInt32(User); }
}

public class CancelReadyMessage : Message
{
    public override byte TypeTag => 8;
    public int User { get; set; }
    public CancelReadyMessage(int user) { User = user; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteInt32(User); }
}

public class CancelGameMessage : Message
{
    public override byte TypeTag => 9;
    public int User { get; set; }
    public CancelGameMessage(int user) { User = user; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteInt32(User); }
}

public class StartPlayingMessage : Message
{
    public override byte TypeTag => 10;
    public StartPlayingMessage() { }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); }
}

public class PlayedMessage : Message
{
    public override byte TypeTag => 11;
    public int User { get; set; }
    public int Score { get; set; }
    public float Accuracy { get; set; }
    public bool FullCombo { get; set; }
    public PlayedMessage(int user, int score, float accuracy, bool fullCombo) { User = user; Score = score; Accuracy = accuracy; FullCombo = fullCombo; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteInt32(User); writer.WriteInt32(Score); writer.WriteSingle(Accuracy); writer.WriteBool(FullCombo); }
}

public class GameEndMessage : Message
{
    public override byte TypeTag => 12;
    public GameEndMessage() { }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); }
}

public class AbortMessage : Message
{
    public override byte TypeTag => 13;
    public int User { get; set; }
    public AbortMessage(int user) { User = user; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteInt32(User); }
}

public class LockRoomMessage : Message
{
    public override byte TypeTag => 14;
    public bool Lock { get; set; }
    public LockRoomMessage(bool lockState) { Lock = lockState; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteBool(Lock); }
}

public class CycleRoomMessage : Message
{
    public override byte TypeTag => 15;
    public bool Cycle { get; set; }
    public CycleRoomMessage(bool cycle) { Cycle = cycle; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteBool(Cycle); }
}

// ============ Client Commands ============

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

public class PingCommand : ClientCommand { public override byte TypeTag => 0; }
public class AuthenticateCommand : ClientCommand { public override byte TypeTag => 1; public Varchar Token { get; } public AuthenticateCommand(Varchar token) { Token = token; } }
public class ChatCommand : ClientCommand { public override byte TypeTag => 2; public Varchar Message { get; } public ChatCommand(Varchar message) { Message = message; } }
public class TouchesCommand : ClientCommand { public override byte TypeTag => 3; public List<TouchFrame> Frames { get; } public TouchesCommand(List<TouchFrame> frames) { Frames = frames; } }
public class JudgesCommand : ClientCommand { public override byte TypeTag => 4; public List<JudgeEvent> Judges { get; } public JudgesCommand(List<JudgeEvent> judges) { Judges = judges; } }
public class CreateRoomCommand : ClientCommand { public override byte TypeTag => 5; public RoomId Id { get; } public CreateRoomCommand(RoomId id) { Id = id; } }
public class JoinRoomCommand : ClientCommand { public override byte TypeTag => 6; public RoomId Id { get; } public bool Monitor { get; } public JoinRoomCommand(RoomId id, bool monitor) { Id = id; Monitor = monitor; } }
public class LeaveRoomCommand : ClientCommand { public override byte TypeTag => 7; }
public class LockRoomCommand : ClientCommand { public override byte TypeTag => 8; public bool Lock { get; } public LockRoomCommand(bool lockState) { Lock = lockState; } }
public class CycleRoomCommand : ClientCommand { public override byte TypeTag => 9; public bool Cycle { get; } public CycleRoomCommand(bool cycle) { Cycle = cycle; } }
public class SelectChartCommand : ClientCommand { public override byte TypeTag => 10; public int Id { get; } public SelectChartCommand(int id) { Id = id; } }
public class RequestStartCommand : ClientCommand { public override byte TypeTag => 11; }
public class ReadyCommand : ClientCommand { public override byte TypeTag => 12; }
public class CancelReadyCommand : ClientCommand { public override byte TypeTag => 13; }
public class PlayedCommand : ClientCommand { public override byte TypeTag => 14; public int Id { get; } public PlayedCommand(int id) { Id = id; } }
public class AbortCommand : ClientCommand { public override byte TypeTag => 15; }

// ============ Server Commands ============

public abstract class ServerCommand : IBinaryData
{
    public abstract byte TypeTag { get; }
    public abstract void WriteBinary(BinaryWriter writer);
}

public class PongCommand : ServerCommand
{
    public override byte TypeTag => 0;
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); }
}

public class AuthenticateResponseCommand : ServerCommand
{
    public override byte TypeTag => 1;
    public bool Success { get; set; }
    public UserInfo? UserInfo { get; set; }
    public ClientRoomState? RoomState { get; set; }
    public string? Error { get; set; }

    public AuthenticateResponseCommand(UserInfo userInfo, ClientRoomState? roomState)
    {
        Success = true;
        UserInfo = userInfo;
        RoomState = roomState;
    }

    public AuthenticateResponseCommand(string error)
    {
        Success = false;
        Error = error;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteBool(Success);
        if (Success)
        {
            UserInfo!.WriteBinary(writer);
            writer.WriteBool(RoomState != null);
            RoomState?.WriteBinary(writer);
        }
        else
        {
            writer.WriteString(Error!);
        }
    }
}

public class ChatResponseCommand : ServerCommand
{
    public override byte TypeTag => 2;
    public bool Success { get; set; }
    public string? Error { get; set; }

    public ChatResponseCommand(bool success, string? error = null)
    {
        Success = success;
        Error = error;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteBool(Success);
        if (!Success)
            writer.WriteString(Error!);
    }
}

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

public class ChangeStateCommand : ServerCommand
{
    public override byte TypeTag => 6;
    public RoomStateData State { get; set; }

    public ChangeStateCommand(RoomStateData state)
    {
        State = state;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        State.WriteBinary(writer);
    }
}

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

public class CreateRoomResponseCommand : ServerCommand
{
    public override byte TypeTag => 8;
    public bool Success { get; set; }
    public string? Error { get; set; }

    public CreateRoomResponseCommand(bool success, string? error = null)
    {
        Success = success;
        Error = error;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteBool(Success);
        if (!Success)
            writer.WriteString(Error!);
    }
}

public class JoinRoomResponseCommand : ServerCommand
{
    public override byte TypeTag => 9;
    public bool Success { get; set; }
    public JoinRoomResponse? Response { get; set; }
    public string? Error { get; set; }

    public JoinRoomResponseCommand(JoinRoomResponse response)
    {
        Success = true;
        Response = response;
    }

    public JoinRoomResponseCommand(string error)
    {
        Success = false;
        Error = error;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteBool(Success);
        if (Success)
            Response!.WriteBinary(writer);
        else
            writer.WriteString(Error!);
    }
}

public class OnJoinRoomCommand : ServerCommand
{
    public override byte TypeTag => 10;
    public UserInfo User { get; set; }

    public OnJoinRoomCommand(UserInfo user)
    {
        User = user;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        User.WriteBinary(writer);
    }
}

public class LeaveRoomResponseCommand : ServerCommand
{
    public override byte TypeTag => 11;
    public bool Success { get; set; }
    public string? Error { get; set; }

    public LeaveRoomResponseCommand(bool success, string? error = null)
    {
        Success = success;
        Error = error;
    }

    public override void WriteBinary(BinaryWriter writer)
    {
        writer.WriteByte(TypeTag);
        writer.WriteBool(Success);
        if (!Success)
            writer.WriteString(Error!);
    }
}

public class LockRoomResponseCommand : ServerCommand
{
    public override byte TypeTag => 12;
    public bool Success { get; set; }
    public string? Error { get; set; }

    public LockRoomResponseCommand(bool success, string? error = null) { Success = success; Error = error; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteBool(Success); if (!Success) writer.WriteString(Error!); }
}

public class CycleRoomResponseCommand : ServerCommand
{
    public override byte TypeTag => 13;
    public bool Success { get; set; }
    public string? Error { get; set; }

    public CycleRoomResponseCommand(bool success, string? error = null) { Success = success; Error = error; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteBool(Success); if (!Success) writer.WriteString(Error!); }
}

public class SelectChartResponseCommand : ServerCommand
{
    public override byte TypeTag => 14;
    public bool Success { get; set; }
    public string? Error { get; set; }

    public SelectChartResponseCommand(bool success, string? error = null) { Success = success; Error = error; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteBool(Success); if (!Success) writer.WriteString(Error!); }
}

public class RequestStartResponseCommand : ServerCommand
{
    public override byte TypeTag => 15;
    public bool Success { get; set; }
    public string? Error { get; set; }

    public RequestStartResponseCommand(bool success, string? error = null) { Success = success; Error = error; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteBool(Success); if (!Success) writer.WriteString(Error!); }
}

public class ReadyResponseCommand : ServerCommand
{
    public override byte TypeTag => 16;
    public bool Success { get; set; }
    public string? Error { get; set; }

    public ReadyResponseCommand(bool success, string? error = null) { Success = success; Error = error; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteBool(Success); if (!Success) writer.WriteString(Error!); }
}

public class CancelReadyResponseCommand : ServerCommand
{
    public override byte TypeTag => 17;
    public bool Success { get; set; }
    public string? Error { get; set; }

    public CancelReadyResponseCommand(bool success, string? error = null) { Success = success; Error = error; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteBool(Success); if (!Success) writer.WriteString(Error!); }
}

public class PlayedResponseCommand : ServerCommand
{
    public override byte TypeTag => 18;
    public bool Success { get; set; }
    public string? Error { get; set; }

    public PlayedResponseCommand(bool success, string? error = null) { Success = success; Error = error; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteBool(Success); if (!Success) writer.WriteString(Error!); }
}

public class AbortResponseCommand : ServerCommand
{
    public override byte TypeTag => 19;
    public bool Success { get; set; }
    public string? Error { get; set; }

    public AbortResponseCommand(bool success, string? error = null) { Success = success; Error = error; }
    public override void WriteBinary(BinaryWriter writer) { writer.WriteByte(TypeTag); writer.WriteBool(Success); if (!Success) writer.WriteString(Error!); }
}
