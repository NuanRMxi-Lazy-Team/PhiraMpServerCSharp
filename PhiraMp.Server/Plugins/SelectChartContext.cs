using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 选歌事件上下文
/// </summary>
public class SelectChartContext : BasePipelineContext
{
    /// <summary>房间对象</summary>
    public IRoom Room { get; }
    
    /// <summary>选歌的用户</summary>
    public IUser User { get; }
    
    /// <summary>选择的谱面信息</summary>
    public ChartInfo Chart { get; }
    
    public SelectChartContext(IRoom room, IUser user, ChartInfo chart)
    {
        Room = room;
        User = user;
        Chart = chart;
    }
}
