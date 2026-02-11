using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using PhiraMp.Core;
using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 基于 MEF 的插件管理器 - 无需特定基类即可发现和管理插件
/// </summary>
public class PluginManager : IDisposable
{
    private readonly ServerState _serverState;
    private readonly string _pluginDirectory;
    private readonly string _configDirectory;
    private readonly string _dataDirectory;
    private readonly Dictionary<string, PluginLoadInfo> _loadedPlugins = new();
    private FileSystemWatcher? _watcher;
    private CompositionContainer? _container;
    private bool _disposed;
    private readonly PluginServiceProvider _serviceProvider = new();

    [ImportMany(typeof(IPluginModule))] public IEnumerable<IPluginModule>? PluginModules { get; set; }

    [ImportMany(typeof(IRoomMessageHandler))]
    public IEnumerable<IRoomMessageHandler>? MessageHandlers { get; set; }

    [ImportMany(typeof(IRoomStateHandler))]
    public IEnumerable<IRoomStateHandler>? StateHandlers { get; set; }

    [ImportMany(typeof(IUserJoinHandler))] public IEnumerable<IUserJoinHandler>? UserJoinHandlers { get; set; }

    [ImportMany(typeof(IUserLeaveHandler))]
    public IEnumerable<IUserLeaveHandler>? UserLeaveHandlers { get; set; }

    [ImportMany(typeof(IRequestStartHandler))]
    public IEnumerable<IRequestStartHandler>? RequestStartHandlers { get; set; }

    [ImportMany(typeof(ISelectChartHandler))]
    public IEnumerable<ISelectChartHandler>? SelectChartHandlers { get; set; }

    [ImportMany(typeof(ICycleModeChangeHandler))]
    public IEnumerable<ICycleModeChangeHandler>? CycleModeChangeHandlers { get; set; }

    [ImportMany(typeof(IJoinRoomRequestHandler))]
    public IEnumerable<IJoinRoomRequestHandler>? JoinRoomRequestHandlers { get; set; }

    [ImportMany(typeof(ICreateRoomRequestHandler))]
    public IEnumerable<ICreateRoomRequestHandler>? CreateRoomRequestHandlers { get; set; }

    [ImportMany(typeof(IAuthenticationHandler))]
    public IEnumerable<IAuthenticationHandler>? AuthenticationHandlers { get; set; }

    public PluginManager(ServerState serverState, string pluginDirectory = "plugins")
    {
        _serverState = serverState;
        _pluginDirectory = Path.GetFullPath(pluginDirectory);
        _configDirectory = Path.Combine(_pluginDirectory, "configs");
        _dataDirectory = Path.Combine(_pluginDirectory, "data");

        // 创建必要的目录
        Directory.CreateDirectory(_pluginDirectory);
        Directory.CreateDirectory(_configDirectory);
        Directory.CreateDirectory(_dataDirectory);
    }

    /// <summary>
    /// 加载所有插件（使用 MEF 发现）
    /// </summary>
    public async Task LoadAllPluginsAsync()
    {
        Logger.Info($"从 {_pluginDirectory} 加载插件");

        try
        {
            // 创建 MEF 目录
            var catalog = new AggregateCatalog();

            var dllFiles = Directory.GetFiles(_pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
            foreach (var dllFile in dllFiles)
            {
                if (!AddPluginToCatalog(dllFile, catalog))
                    continue;
            }

            // 创建组合容器
            _container = new CompositionContainer(catalog);

            // 组合此实例以获取所有导入
            _container.SatisfyImportsOnce(this);

            // 初始化所有插件模块
            await InitializePluginModulesAsync();

            // 打印加载信息
            LogPluginLoadSummary();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "加载插件时发生错误:");
        }

        // 启用热重载
        EnableHotReload();
    }

    /// <summary>
    /// 将插件添加到目录中
    /// </summary>
    private bool AddPluginToCatalog(string dllFile, AggregateCatalog catalog)
    {
        try
        {
            var assembly = Assembly.LoadFrom(dllFile);
            var assemblyCatalog = new AssemblyCatalog(assembly);
            catalog.Catalogs.Add(assemblyCatalog);

            _loadedPlugins[Path.GetFileName(dllFile)] = new PluginLoadInfo
            {
                Path = dllFile,
                Assembly = assembly,
                LoadTime = DateTime.UtcNow
            };

            Logger.Info($"已编目插件: {Path.GetFileName(dllFile)}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"编目插件失败 {dllFile}:");
            return false;
        }
    }

    /// <summary>
    /// 初始化所有插件模块
    /// </summary>
    private async Task InitializePluginModulesAsync()
    {
        if (PluginModules == null) return;

        foreach (var module in PluginModules)
        {
            try
            {
                var pluginName = module.GetType().Name;
                var logger = new PluginLogger(pluginName);
                var config = new PluginConfig(_configDirectory, logger);
                var api = new PluginAPI(_serverState, logger);

                var context = new PluginContext(
                    _serverState,
                    _pluginDirectory,
                    _configDirectory,
                    _dataDirectory,
                    api,
                    logger,
                    config,
                    _serviceProvider);

                await module.InitializeAsync(context);
                Logger.Info($"已初始化插件模块: {pluginName}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"初始化插件失败 {module.GetType().Name}:");
            }
        }
    }

    /// <summary>
    /// 打印插件加载摘要
    /// </summary>
    private void LogPluginLoadSummary()
    {
        Logger.Info($"已加载 {_loadedPlugins.Count} 个插件");
    }

    /// <summary>
    /// 启用插件热重载
    /// </summary>
    private void EnableHotReload()
    {
        try
        {
            _watcher = new FileSystemWatcher(_pluginDirectory)
            {
                Filter = "*.dll",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };

            _watcher.Changed += OnPluginFileChanged;
            _watcher.Created += OnPluginFileChanged;
            _watcher.Deleted += OnPluginFileChanged;
            _watcher.EnableRaisingEvents = true;

            Logger.Info("已启用插件热重载");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "启用热重载失败:");
        }
    }

    /// <summary>
    /// 插件文件变化事件处理
    /// </summary>
    private void OnPluginFileChanged(object sender, FileSystemEventArgs e)
    {
        Logger.Info($"检测到插件变化: {e.Name} ({e.ChangeType})");

        // 延迟重新加载以确保文件写入完成
        Task.Run(async () =>
        {
            await Task.Delay(1000);
            Logger.Info("正在重新加载插件...");
            await ReloadPluginsAsync();
        });
    }

    /// <summary>
    /// 重新加载所有插件
    /// </summary>
    public async Task ReloadPluginsAsync()
    {
        try
        {
            // 关闭所有现有插件
            if (PluginModules != null)
            {
                foreach (var module in PluginModules)
                {
                    try
                    {
                        await module.ShutdownAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"关闭插件失败 {module.GetType().Name}:");
                    }
                }
            }

            // 清理容器
            _container?.Dispose();
            _loadedPlugins.Clear();

            // 清理插件服务
            _serviceProvider.ClearServices();

            // 重新加载
            await LoadAllPluginsAsync();

            Logger.Info("插件重新加载完成");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "重新加载插件时发生错误:");
        }
    }

    // ===== 事件分发方法 =====

    /// <summary>
    /// 分发房间消息到所有处理器
    /// </summary>
    /// <returns>如果插件已处理返回 true，否则返回 false</returns>
    public async Task<bool> DispatchRoomMessageAsync(Room room, User user, string message)
    {
        if (MessageHandlers == null) return false;

        var context = new RoomMessageContext(room, user, message);
        var adapters = MessageHandlers.Select(h => new RoomMessageHandlerAdapter(h));

        await PipelineExecutor.ExecuteAsync(adapters, context);
        return context.IsHandled;
    }

    /// <summary>
    /// 分发房间状态变化到所有处理器
    /// </summary>
    /// <returns>如果插件已处理返回 true，否则返回 false</returns>
    public async Task<bool> DispatchRoomStateChangeAsync(Room room, string newState)
    {
        if (StateHandlers == null) return false;

        var context = new RoomStateContext(room, newState);
        var adapters = StateHandlers.Select(h => new RoomStateHandlerAdapter(h));

        await PipelineExecutor.ExecuteAsync(adapters, context);
        return context.IsHandled;
    }

    /// <summary>
    /// 分发用户加入到所有处理器
    /// </summary>
    /// <returns>如果插件已处理返回 true，否则返回 false</returns>
    public async Task<bool> DispatchUserJoinAsync(Room room, User user)
    {
        if (UserJoinHandlers == null) return false;

        var context = new UserEventContext(room, user);
        var adapters = UserJoinHandlers.Select(h => new UserJoinHandlerAdapter(h));

        await PipelineExecutor.ExecuteAsync(adapters, context);
        return context.IsHandled;
    }

    /// <summary>
    /// 分发用户离开到所有处理器
    /// </summary>
    /// <returns>如果插件已处理返回 true，否则返回 false</returns>
    public async Task<bool> DispatchUserLeaveAsync(Room room, User user)
    {
        if (UserLeaveHandlers == null) return false;

        var context = new UserEventContext(room, user);
        var adapters = UserLeaveHandlers.Select(h => new UserLeaveHandlerAdapter(h));

        await PipelineExecutor.ExecuteAsync(adapters, context);
        return context.IsHandled;
    }

    /// <summary>
    /// 分发游戏开始请求到所有处理器 - 插件可以抛出异常来阻止开始
    /// </summary>
    /// <returns>如果插件已处理返回 true，否则返回 false</returns>
    public async Task<bool> DispatchRequestStartAsync(Room room, User user)
    {
        if (RequestStartHandlers == null) return false;

        var context = new RequestStartContext(room, user);
        var adapters = RequestStartHandlers.Select(h => new RequestStartHandlerAdapter(h));

        await PipelineExecutor.ExecuteWithValidationAsync(adapters, context);
        return context.IsHandled;
    }

    /// <summary>
    /// 分发选歌到所有处理器
    /// </summary>
    /// <returns>如果插件已处理返回 true，否则返回 false</returns>
    public async Task<bool> DispatchSelectChartAsync(Room room, User user, ChartInfo chart)
    {
        if (SelectChartHandlers == null) return false;

        var context = new SelectChartContext(room, user, chart);
        var adapters = SelectChartHandlers.Select(h => new SelectChartHandlerAdapter(h));

        await PipelineExecutor.ExecuteAsync(adapters, context);
        return context.IsHandled;
    }

    /// <summary>
    /// 分发循环模式变化到所有处理器
    /// </summary>
    /// <returns>如果插件已处理返回 true，否则返回 false</returns>
    public async Task<bool> DispatchCycleModeChangeAsync(Room room, User user, bool cycleEnabled)
    {
        if (CycleModeChangeHandlers == null) return false;

        var context = new CycleModeChangeContext(room, user, cycleEnabled);
        var adapters = CycleModeChangeHandlers.Select(h => new CycleModeChangeHandlerAdapter(h));

        await PipelineExecutor.ExecuteAsync(adapters, context);
        return context.IsHandled;
    }

    /// <summary>
    /// 分发加入房间请求到所有处理器 - 插件可以修改目标房间或抛出异常阻止
    /// </summary>
    /// <returns>如果插件已处理返回 true，否则返回 false</returns>
    public async Task<bool> DispatchJoinRoomRequestAsync(User user, RoomId roomId, bool monitor)
    {
        if (JoinRoomRequestHandlers == null) return false;

        var context = new JoinRoomRequestContext(user, roomId, monitor);
        var adapters = JoinRoomRequestHandlers.Select(h => new JoinRoomRequestHandlerAdapter(h));

        await PipelineExecutor.ExecuteWithValidationAsync(adapters, context);
        return context.IsHandled;
    }

    /// <summary>
    /// 分发创建房间请求到所有处理器 - 插件可以抛出异常阻止创建
    /// </summary>
    /// <returns>如果插件已处理返回 true，否则返回 false</returns>
    public async Task<bool> DispatchCreateRoomRequestAsync(User user, RoomId roomId)
    {
        if (CreateRoomRequestHandlers == null) return false;

        var context = new CreateRoomRequestContext(user, roomId);
        var adapters = CreateRoomRequestHandlers.Select(h => new CreateRoomRequestHandlerAdapter(h));

        await PipelineExecutor.ExecuteWithValidationAsync(adapters, context);
        return context.IsHandled;
    }

    /// <summary>
    /// 分发鉴权到所有处理器 - 插件可以验证、修改用户信息或抛出异常阻止鉴权
    /// </summary>
    /// <returns>可能被插件修改的用户信息</returns>
    public async Task<PhiraUserInfo> DispatchAuthenticationAsync(string token, PhiraUserInfo userInfo, Guid sessionId)
    {
        if (AuthenticationHandlers == null) return userInfo;

        var context = new AuthenticationContext(token, userInfo, sessionId);
        var adapters = AuthenticationHandlers.Select(h => new AuthenticationHandlerAdapter(h))
            .Cast<IPipelineHandler<AuthenticationContext>>();

        await PipelineExecutor.ExecuteWithValidationAsync(adapters, context);

        // 返回可能被插件修改的用户信息
        return context.UserInfo;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);

        _watcher?.Dispose();

        // 关闭所有插件
        if (PluginModules != null)
        {
            foreach (var module in PluginModules)
            {
                try
                {
                    module.ShutdownAsync().Wait();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"关闭插件错误 {module.GetType().Name}:");
                }
            }
        }

        _container?.Dispose();
        _loadedPlugins.Clear();
    }
}

/// <summary>
/// 插件加载信息
/// </summary>
public class PluginLoadInfo
{
    /// <summary>插件文件路径</summary>
    public required string Path { get; init; }

    /// <summary>插件程序集</summary>
    public required Assembly Assembly { get; init; }

    /// <summary>加载时间</summary>
    public DateTime LoadTime { get; init; }
}