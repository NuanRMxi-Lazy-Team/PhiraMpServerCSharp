namespace PhiraMp.Core;

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
