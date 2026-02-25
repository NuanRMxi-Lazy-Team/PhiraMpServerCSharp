namespace PhiraMp.Server.Plugins;

/// <summary>
/// 插件日志接口 - 为插件提供统一的日志记录功能
/// </summary>
public interface IPluginLogger
{
    /// <summary>
    /// 记录调试信息
    /// </summary>
    void Debug(string message);
    
    /// <summary>
    /// 记录普通信息
    /// </summary>
    void Info(string message);
    
    /// <summary>
    /// 记录警告信息
    /// </summary>
    void Warning(string message);
    
    /// <summary>
    /// 记录错误信息
    /// </summary>
    void Error(string message);
    
    /// <summary>
    /// 记录错误信息和异常
    /// </summary>
    void Error(Exception ex, string message);
}

