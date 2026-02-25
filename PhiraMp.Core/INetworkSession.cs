namespace PhiraMp.Core;

/// <summary>
/// 网络会话抽象接口，供逻辑层使用，与具体网络实现解耦
/// </summary>
public interface INetworkSession : IDisposable
{
    /// <summary>协议版本号</summary>
    byte Version { get; }

    /// <summary>最后一次收到数据的时间</summary>
    DateTime LastReceive { get; }

    /// <summary>连接是否仍然有效</summary>
    bool IsConnected { get; }

    /// <summary>向客户端发送服务端命令</summary>
    Task SendAsync(ServerCommand cmd);
}
