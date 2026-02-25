namespace PhiraMp.Server.Plugins;

/// <summary>
/// 选歌处理器接口 - 导出此接口以处理选歌
/// 使用 [Export(typeof(ISelectChartHandler))] 注册
/// </summary>
public interface ISelectChartHandler
{
    Task HandleSelectChartAsync(SelectChartContext context);
}
