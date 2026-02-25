namespace PhiraMp.Core;

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

    public ClientRoomState(RoomId id, RoomStateData state, bool live, bool locked, bool cycle, bool isHost,
        bool isReady, Dictionary<int, UserInfo> users)
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
