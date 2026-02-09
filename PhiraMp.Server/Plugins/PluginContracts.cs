using System.ComponentModel.Composition;
using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 插件模块接口（可选）- 插件不一定需要实现此接口
/// 使用 [Export(typeof(IPluginModule))] 让 MEF 发现插件
/// </summary>
public interface IPluginModule
{
    /// <summary>
    /// 插件加载时调用（可选 - 需要时实现）
    /// </summary>
    Task InitializeAsync(PluginContext context);
    
    /// <summary>
    /// 插件卸载时调用（可选 - 需要时实现）
    /// </summary>
    Task ShutdownAsync();
}

/// <summary>
/// 房间消息处理器接口 - 导出此接口以处理房间消息
/// 使用 [Export(typeof(IRoomMessageHandler))] 注册
/// </summary>
public interface IRoomMessageHandler
{
    Task HandleMessageAsync(RoomMessageContext context);
}

/// <summary>
/// 房间状态变化处理器接口 - 导出此接口以处理状态变化
/// 使用 [Export(typeof(IRoomStateHandler))] 注册
/// </summary>
public interface IRoomStateHandler
{
    Task HandleStateChangeAsync(RoomStateContext context);
}

/// <summary>
/// 用户加入处理器接口 - 导出此接口以处理用户加入
/// 使用 [Export(typeof(IUserJoinHandler))] 注册
/// </summary>
public interface IUserJoinHandler
{
    Task HandleUserJoinAsync(UserEventContext context);
}

/// <summary>
/// 用户离开处理器接口 - 导出此接口以处理用户离开
/// 使用 [Export(typeof(IUserLeaveHandler))] 注册
/// </summary>
public interface IUserLeaveHandler
{
    Task HandleUserLeaveAsync(UserEventContext context);
}

/// <summary>
/// 请求开始游戏处理器接口 - 导出此接口以验证/修改开始请求
/// 使用 [Export(typeof(IRequestStartHandler))] 注册
/// 插件可以抛出异常来阻止游戏开始
/// </summary>
public interface IRequestStartHandler
{
    Task HandleRequestStartAsync(RequestStartContext context);
}

/// <summary>
/// 选歌处理器接口 - 导出此接口以处理选歌
/// 使用 [Export(typeof(ISelectChartHandler))] 注册
/// </summary>
public interface ISelectChartHandler
{
    Task HandleSelectChartAsync(SelectChartContext context);
}

/// <summary>
/// 循环模式变化处理器接口 - 导出此接口以处理循环模式变化
/// 使用 [Export(typeof(ICycleModeChangeHandler))] 注册
/// </summary>
public interface ICycleModeChangeHandler
{
    Task HandleCycleModeChangeAsync(CycleModeChangeContext context);
}

/// <summary>
/// 加入房间请求处理器接口 - 导出此接口以拦截加入房间请求
/// 使用 [Export(typeof(IJoinRoomRequestHandler))] 注册
/// 插件可以修改目标房间 ID 或抛出异常来阻止加入
/// </summary>
public interface IJoinRoomRequestHandler
{
    Task HandleJoinRoomRequestAsync(JoinRoomRequestContext context);
}

/// <summary>
/// 创建房间请求处理器接口 - 导出此接口以拦截创建房间请求
/// 使用 [Export(typeof(ICreateRoomRequestHandler))] 注册
/// 插件可以抛出异常来阻止房间创建
/// </summary>
public interface ICreateRoomRequestHandler
{
    Task HandleCreateRoomRequestAsync(CreateRoomRequestContext context);
}

/// <summary>
/// 插件上下文 - 在插件初始化时提供
/// </summary>
public class PluginContext
{
    /// <summary>
    /// 服务器状态（直接访问，高级功能）
    /// </summary>
    public ServerState ServerState { get; }
    
    /// <summary>
    /// 插件目录路径
    /// </summary>
    public string PluginDirectory { get; }
    
    /// <summary>
    /// 配置文件目录路径
    /// </summary>
    public string ConfigDirectory { get; }
    
    /// <summary>
    /// 数据文件目录路径
    /// </summary>
    public string DataDirectory { get; }
    
    /// <summary>
    /// 插件 API - 推荐使用此接口操作服务器
    /// </summary>
    public IPluginAPI API { get; }
    
    /// <summary>
    /// 插件日志记录器
    /// </summary>
    public IPluginLogger Logger { get; }
    
    /// <summary>
    /// 插件配置管理器
    /// </summary>
    public IPluginConfig Config { get; }
    
    public PluginContext(
        ServerState serverState, 
        string pluginDir, 
        string configDir, 
        string dataDir,
        IPluginAPI api,
        IPluginLogger logger,
        IPluginConfig config)
    {
        ServerState = serverState;
        PluginDirectory = pluginDir;
        ConfigDirectory = configDir;
        DataDirectory = dataDir;
        API = api;
        Logger = logger;
        Config = config;
    }
}

/// <summary>
/// 房间消息事件上下文
/// </summary>
public class RoomMessageContext
{
    /// <summary>房间对象</summary>
    public Room Room { get; }
    
    /// <summary>发送消息的用户</summary>
    public User User { get; }
    
    /// <summary>消息内容</summary>
    public string Message { get; }
    
    public RoomMessageContext(Room room, User user, string message)
    {
        Room = room;
        User = user;
        Message = message;
    }
}

/// <summary>
/// 房间状态变化事件上下文
/// </summary>
public class RoomStateContext
{
    /// <summary>房间对象</summary>
    public Room Room { get; }
    
    /// <summary>新状态</summary>
    public string NewState { get; }
    
    public RoomStateContext(Room room, string newState)
    {
        Room = room;
        NewState = newState;
    }
}

/// <summary>
/// 用户加入/离开事件上下文
/// </summary>
public class UserEventContext
{
    /// <summary>房间对象</summary>
    public Room Room { get; }
    
    /// <summary>用户对象</summary>
    public User User { get; }
    
    public UserEventContext(Room room, User user)
    {
        Room = room;
        User = user;
    }
}

/// <summary>
/// 请求开始游戏事件上下文
/// </summary>
public class RequestStartContext
{
    /// <summary>房间对象</summary>
    public Room Room { get; }
    
    /// <summary>发起请求的用户</summary>
    public User User { get; }
    
    public RequestStartContext(Room room, User user)
    {
        Room = room;
        User = user;
    }
}

/// <summary>
/// 选歌事件上下文
/// </summary>
public class SelectChartContext
{
    /// <summary>房间对象</summary>
    public Room Room { get; }
    
    /// <summary>选歌的用户</summary>
    public User User { get; }
    
    /// <summary>选择的谱面信息</summary>
    public ChartInfo Chart { get; }
    
    public SelectChartContext(Room room, User user, ChartInfo chart)
    {
        Room = room;
        User = user;
        Chart = chart;
    }
}

/// <summary>
/// 循环模式变化事件上下文
/// </summary>
public class CycleModeChangeContext
{
    /// <summary>房间对象</summary>
    public Room Room { get; }
    
    /// <summary>操作的用户</summary>
    public User User { get; }
    
    /// <summary>是否启用循环模式</summary>
    public bool CycleEnabled { get; }
    
    public CycleModeChangeContext(Room room, User user, bool cycleEnabled)
    {
        Room = room;
        User = user;
        CycleEnabled = cycleEnabled;
    }
}

/// <summary>
/// 加入房间请求事件上下文
/// </summary>
public class JoinRoomRequestContext
{
    /// <summary>发起请求的用户</summary>
    public User User { get; }
    
    /// <summary>原始请求的房间 ID</summary>
    public RoomId OriginalRoomId { get; }
    
    /// <summary>目标房间 ID（插件可以修改此值来重定向用户）</summary>
    public RoomId TargetRoomId { get; set; }
    
    /// <summary>是否为监视模式</summary>
    public bool Monitor { get; }
    
    public JoinRoomRequestContext(User user, RoomId roomId, bool monitor)
    {
        User = user;
        OriginalRoomId = roomId;
        TargetRoomId = roomId;
        Monitor = monitor;
    }
}

/// <summary>
/// 创建房间请求事件上下文
/// </summary>
public class CreateRoomRequestContext
{
    /// <summary>发起请求的用户</summary>
    public User User { get; }
    
    /// <summary>请求创建的房间 ID</summary>
    public RoomId RoomId { get; }
    
    public CreateRoomRequestContext(User user, RoomId roomId)
    {
        User = user;
        RoomId = roomId;
    }
}

