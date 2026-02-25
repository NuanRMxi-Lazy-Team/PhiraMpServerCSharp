namespace PhiraMp.Core;

/// <summary>
/// 用户抽象接口，供插件和外部层使用，与具体 User 实现解耦
/// </summary>
public interface IUser
{
    /// <summary>用户 ID</summary>
    int Id { get; }

    /// <summary>用户名</summary>
    string Name { get; }

    /// <summary>用户语言</summary>
    string Language { get; }

    /// <summary>当前所在房间（null 表示不在任何房间）</summary>
    IRoom? Room { get; set; }

    /// <summary>是否为监视者</summary>
    bool IsMonitor { get; set; }

    /// <summary>游戏进度时间戳</summary>
    float GameTime { get; set; }

    /// <summary>转为协议用 UserInfo</summary>
    UserInfo ToInfo();

    /// <summary>尝试向用户发送服务端命令</summary>
    Task TrySendAsync(ServerCommand cmd);

    /// <summary>当前用户是否有监视权限</summary>
    bool CanMonitor();
}
