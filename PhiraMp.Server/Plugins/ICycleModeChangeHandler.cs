namespace PhiraMp.Server.Plugins;

/// <summary>
/// 循环模式变化处理器接口 - 导出此接口以处理循环模式变化
/// 使用 [Export(typeof(ICycleModeChangeHandler))] 注册
/// </summary>
public interface ICycleModeChangeHandler
{
    Task HandleCycleModeChangeAsync(CycleModeChangeContext context);
}
