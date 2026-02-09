namespace PhiraMp.Plugins.RandomRoom;

/// <summary>
/// 随机房间插件配置
/// </summary>
public class RandomRoomConfig
{
    /// <summary>保留的房间名列表</summary>
    public List<string> ReservedRoomNames { get; set; } = new();

    /// <summary>是否启用重定向消息</summary>
    public bool RedirectMessageEnabled { get; set; } = true;

    /// <summary>重定向消息内容</summary>
    public string RedirectMessage { get; set; } = string.Empty;
}