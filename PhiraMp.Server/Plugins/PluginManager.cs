using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using PhiraMp.Server.Models;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// MEF-based plugin manager - discovers and manages plugins without requiring specific base classes
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

    [ImportMany(typeof(IPluginModule))]
    public IEnumerable<IPluginModule>? PluginModules { get; set; }

    [ImportMany(typeof(IRoomMessageHandler))]
    public IEnumerable<IRoomMessageHandler>? MessageHandlers { get; set; }

    [ImportMany(typeof(IRoomStateHandler))]
    public IEnumerable<IRoomStateHandler>? StateHandlers { get; set; }

    [ImportMany(typeof(IUserJoinHandler))]
    public IEnumerable<IUserJoinHandler>? UserJoinHandlers { get; set; }

    [ImportMany(typeof(IUserLeaveHandler))]
    public IEnumerable<IUserLeaveHandler>? UserLeaveHandlers { get; set; }

    [ImportMany(typeof(IRequestStartHandler))]
    public IEnumerable<IRequestStartHandler>? RequestStartHandlers { get; set; }

    [ImportMany(typeof(ISelectChartHandler))]
    public IEnumerable<ISelectChartHandler>? SelectChartHandlers { get; set; }

    [ImportMany(typeof(ICycleModeChangeHandler))]
    public IEnumerable<ICycleModeChangeHandler>? CycleModeChangeHandlers { get; set; }

    public PluginManager(ServerState serverState, string pluginDirectory = "plugins")
    {
        _serverState = serverState;
        _pluginDirectory = Path.GetFullPath(pluginDirectory);
        _configDirectory = Path.Combine(_pluginDirectory, "configs");
        _dataDirectory = Path.Combine(_pluginDirectory, "data");

        Directory.CreateDirectory(_pluginDirectory);
        Directory.CreateDirectory(_configDirectory);
        Directory.CreateDirectory(_dataDirectory);
    }

    /// <summary>
    /// Load all plugins using MEF discovery
    /// </summary>
    public async Task LoadAllPluginsAsync()
    {
        Logger.Info($"Loading plugins from {_pluginDirectory}");

        try
        {
            // Create MEF catalog from plugin directory
            var catalog = new AggregateCatalog();
            
            var dllFiles = Directory.GetFiles(_pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
            foreach (var dllFile in dllFiles)
            {
                try
                {
                    // Use DirectoryCatalog for each DLL
                    var assembly = Assembly.LoadFrom(dllFile);
                    var assemblyCatalog = new AssemblyCatalog(assembly);
                    catalog.Catalogs.Add(assemblyCatalog);
                    
                    _loadedPlugins[Path.GetFileName(dllFile)] = new PluginLoadInfo
                    {
                        Path = dllFile,
                        Assembly = assembly,
                        LoadTime = DateTime.UtcNow
                    };
                    
                    Logger.Info($"Cataloged plugin: {Path.GetFileName(dllFile)}");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to catalog {dllFile}:");
                }
            }

            // Create composition container
            _container = new CompositionContainer(catalog);
            
            // Compose this instance to get all imports
            _container.SatisfyImportsOnce(this);

            // Initialize all plugin modules
            if (PluginModules != null)
            {
                var context = new PluginContext(_serverState, _pluginDirectory, _configDirectory, _dataDirectory);
                foreach (var module in PluginModules)
                {
                    try
                    {
                        await module.InitializeAsync(context);
                        Logger.Info($"Initialized plugin module: {module.GetType().Name}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, $"Failed to initialize plugin {module.GetType().Name}:");
                    }
                }
            }

            Logger.Info($"Loaded {_loadedPlugins.Count} plugins with MEF");
            Logger.Info($"  - {PluginModules?.Count() ?? 0} modules");
            Logger.Info($"  - {MessageHandlers?.Count() ?? 0} message handlers");
            Logger.Info($"  - {StateHandlers?.Count() ?? 0} state handlers");
            Logger.Info($"  - {UserJoinHandlers?.Count() ?? 0} user join handlers");
            Logger.Info($"  - {UserLeaveHandlers?.Count() ?? 0} user leave handlers");
            Logger.Info($"  - {RequestStartHandlers?.Count() ?? 0} request start handlers");
            Logger.Info($"  - {SelectChartHandlers?.Count() ?? 0} select chart handlers");
            Logger.Info($"  - {CycleModeChangeHandlers?.Count() ?? 0} cycle mode change handlers");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load plugins:");
        }
    }

    /// <summary>
    /// Reload all plugins
    /// </summary>
    public async Task ReloadAllPluginsAsync()
    {
        Logger.Info("Reloading all plugins...");
        
        // Shutdown current plugins
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
                    Logger.Error(ex, $"Error shutting down plugin {module.GetType().Name}:");
                }
            }
        }

        // Dispose current container
        _container?.Dispose();
        _container = null;
        
        // Clear plugin info
        _loadedPlugins.Clear();
        
        // Small delay to ensure files are released
        await Task.Delay(500);
        
        // GC to help unload assemblies
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Reload
        await LoadAllPluginsAsync();
    }

    /// <summary>
    /// Enable hot reload monitoring
    /// </summary>
    public void EnableHotReload()
    {
        if (_watcher != null)
            return;

        _watcher = new FileSystemWatcher(_pluginDirectory, "*.dll")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnPluginFileChanged;
        _watcher.Created += OnPluginFileCreated;

        Logger.Info("Hot reload enabled for plugins");
    }

    private void OnPluginFileChanged(object sender, FileSystemEventArgs e)
    {
        Logger.Info($"Plugin file changed: {Path.GetFileName(e.FullPath)}");
        Task.Run(async () =>
        {
            await Task.Delay(1000); // Debounce
            await ReloadAllPluginsAsync();
        });
    }

    private void OnPluginFileCreated(object sender, FileSystemEventArgs e)
    {
        Logger.Info($"Plugin file created: {Path.GetFileName(e.FullPath)}");
        Task.Run(async () =>
        {
            await Task.Delay(1000); // Debounce
            await ReloadAllPluginsAsync();
        });
    }

    /// <summary>
    /// Dispatch room message to all handlers
    /// </summary>
    public async Task DispatchRoomMessageAsync(Room room, User user, string message)
    {
        if (MessageHandlers == null) return;

        var context = new RoomMessageContext(room, user, message);
        foreach (var handler in MessageHandlers)
        {
            try
            {
                await handler.HandleMessageAsync(context);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error in message handler {handler.GetType().Name}:");
            }
        }
    }

    /// <summary>
    /// Dispatch room state change to all handlers
    /// </summary>
    public async Task DispatchRoomStateChangeAsync(Room room, string newState)
    {
        if (StateHandlers == null) return;

        var context = new RoomStateContext(room, newState);
        foreach (var handler in StateHandlers)
        {
            try
            {
                await handler.HandleStateChangeAsync(context);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error in state handler {handler.GetType().Name}:");
            }
        }
    }

    /// <summary>
    /// Dispatch user join to all handlers
    /// </summary>
    public async Task DispatchUserJoinAsync(Room room, User user)
    {
        if (UserJoinHandlers == null) return;

        var context = new UserEventContext(room, user);
        foreach (var handler in UserJoinHandlers)
        {
            try
            {
                await handler.HandleUserJoinAsync(context);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error in user join handler {handler.GetType().Name}:");
            }
        }
    }

    /// <summary>
    /// Dispatch user leave to all handlers
    /// </summary>
    public async Task DispatchUserLeaveAsync(Room room, User user)
    {
        if (UserLeaveHandlers == null) return;

        var context = new UserEventContext(room, user);
        foreach (var handler in UserLeaveHandlers)
        {
            try
            {
                await handler.HandleUserLeaveAsync(context);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error in user leave handler {handler.GetType().Name}:");
            }
        }
    }

    /// <summary>
    /// Dispatch request start to all handlers - plugins can throw exceptions to prevent start
    /// </summary>
    public async Task DispatchRequestStartAsync(Room room, User user)
    {
        if (RequestStartHandlers == null) return;

        var context = new RequestStartContext(room, user);
        foreach (var handler in RequestStartHandlers)
        {
            try
            {
                await handler.HandleRequestStartAsync(context);
            }
            catch
            {
                // Re-throw exceptions from plugins to allow them to prevent start
                throw;
            }
        }
    }

    /// <summary>
    /// Dispatch select chart to all handlers
    /// </summary>
    public async Task DispatchSelectChartAsync(Room room, User user, ChartInfo chart)
    {
        if (SelectChartHandlers == null) return;

        var context = new SelectChartContext(room, user, chart);
        foreach (var handler in SelectChartHandlers)
        {
            try
            {
                await handler.HandleSelectChartAsync(context);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error in select chart handler {handler.GetType().Name}:");
            }
        }
    }

    /// <summary>
    /// Dispatch cycle mode change to all handlers
    /// </summary>
    public async Task DispatchCycleModeChangeAsync(Room room, User user, bool cycleEnabled)
    {
        if (CycleModeChangeHandlers == null) return;

        var context = new CycleModeChangeContext(room, user, cycleEnabled);
        foreach (var handler in CycleModeChangeHandlers)
        {
            try
            {
                await handler.HandleCycleModeChangeAsync(context);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error in cycle mode change handler {handler.GetType().Name}:");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);

        _watcher?.Dispose();

        // Shutdown all plugins
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
                    Logger.Error(ex, $"Error shutting down plugin {module.GetType().Name}:");
                }
            }
        }

        _container?.Dispose();
        _loadedPlugins.Clear();
    }
}

/// <summary>
/// Information about a loaded plugin
/// </summary>
internal class PluginLoadInfo
{
    public string Path { get; set; } = "";
    public Assembly? Assembly { get; set; }
    public DateTime LoadTime { get; set; }
}
