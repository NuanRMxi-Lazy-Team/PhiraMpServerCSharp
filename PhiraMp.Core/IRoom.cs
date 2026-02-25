namespace PhiraMp.Core;

/// <summary>
/// 房间抽象接口，供插件和外部层使用，与具体 Room 实现解耦
/// </summary>
public interface IRoom
{
    /// <summary>房间 ID</summary>
    RoomId Id { get; }

    /// <summary>房主</summary>
    IUser Host { get; }

    /// <summary>是否处于直播（监视）模式</summary>
    bool Live { get; set; }

    /// <summary>是否已锁定（禁止新用户加入）</summary>
    bool Locked { get; set; }

    /// <summary>是否开启循环换庄模式</summary>
    bool Cycle { get; }

    /// <summary>当前选中的谱面（null 表示未选）</summary>
    ChartInfo? Chart { get; set; }

    /// <summary>获取房间状态（供客户端协议使用）</summary>
    RoomStateData GetClientRoomState();

    /// <summary>获取所有非监视玩家</summary>
    List<IUser> GetUsers();

    /// <summary>获取所有监视者</summary>
    List<IUser> GetMonitors();

    /// <summary>获取房间内所有用户（玩家 + 监视者）</summary>
    List<IUser> GetAllUsers();

    /// <summary>向房间所有人广播一条 Message 封装命令</summary>
    Task SendAsync(Message msg);

    /// <summary>向房间所有人广播服务端命令</summary>
    Task BroadcastAsync(ServerCommand cmd);

    /// <summary>仅向监视者广播服务端命令</summary>
    Task BroadcastMonitorsAsync(ServerCommand cmd);

    /// <summary>以指定用户身份向房间发送聊天消息</summary>
    Task SendAsAsync(IUser user, string content);

    /// <summary>处理用户离开，返回 true 表示房间应当被销毁</summary>
    Task<bool> OnUserLeaveAsync(IUser user);

    /// <summary>将用户加入房间，返回 false 表示房间已满</summary>
    bool AddUser(IUser user, bool monitor);

    /// <summary>判断指定用户是否为房主</summary>
    bool IsHost(IUser user);

    /// <summary>检查指定用户是否为房主，不是则抛出异常</summary>
    void CheckHost(IUser user);

    /// <summary>检查指定用户是否有权选歌，无权则抛出异常</summary>
    void CheckCanSelectChart(IUser user);

    /// <summary>设置循环换庄模式</summary>
    void SetCycle(bool cycle);

    /// <summary>向房间广播状态变化通知（包含插件回调）</summary>
    Task OnStateChangeAsync();
}
