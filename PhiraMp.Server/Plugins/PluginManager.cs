using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using PhiraMp.Plugin.SDK;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// Plugin manager handles loading, unloading, and reloading plugins with isolation
/// </summary>
public class PluginManager : IDisposable
{
    private readonly ConcurrentDictionary<string, PluginContainer> _plugins = new();
    private readonly IServerAPI _serverAPI;
    private readonly string _pluginDirectory;
    private readonly string _configDirectory;
    private readonly string _dataDirectory;
    private FileSystemWatcher? _watcher;
    private bool _disposed;

    public PluginManager(IServerAPI serverAPI, string pluginDirectory = "plugins")
    {
        _serverAPI = serverAPI;
        _pluginDirectory = Path.GetFullPath(pluginDirectory);
        _configDirectory = Path.Combine(_pluginDirectory, "configs");
        _dataDirectory = Path.Combine(_pluginDirectory, "data");

        Directory.CreateDirectory(_pluginDirectory);
        Directory.CreateDirectory(_configDirectory);
        Directory.CreateDirectory(_dataDirectory);
    }

    /// <summary>
    /// Load all plugins from the plugin directory
    /// </summary>
    public async Task LoadAllPluginsAsync()
    {
        Logger.Info($"Loading plugins from {_pluginDirectory}");

        var dllFiles = Directory.GetFiles(_pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
        foreach (var dllFile in dllFiles)
        {
            await LoadPluginAsync(dllFile);
        }

        Logger.Info($"Loaded {_plugins.Count} plugins");
    }

    /// <summary>
    /// Load a single plugin from a DLL file
    /// </summary>
    public async Task<bool> LoadPluginAsync(string dllPath)
    {
        try
        {
            var fileName = Path.GetFileName(dllPath);
            if (_plugins.ContainsKey(fileName))
            {
                Logger.Warning($"Plugin {fileName} is already loaded");
                return false;
            }

            Logger.Info($"Loading plugin: {fileName}");

            // Create isolated load context for the plugin
            var loadContext = new PluginLoadContext(dllPath);
            var assembly = loadContext.LoadFromAssemblyPath(dllPath);

            // Find plugin classes
            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .ToList();

            if (pluginTypes.Count == 0)
            {
                Logger.Warning($"No plugin classes found in {fileName}");
                loadContext.Unload();
                return false;
            }

            if (pluginTypes.Count > 1)
            {
                Logger.Warning($"Multiple plugin classes found in {fileName}, using the first one");
            }

            var pluginType = pluginTypes[0];
            var plugin = (IPlugin?)Activator.CreateInstance(pluginType);
            
            if (plugin == null)
            {
                Logger.Error($"Failed to create plugin instance from {fileName}");
                loadContext.Unload();
                return false;
            }

            // Create plugin context
            var context = new PluginContext(_serverAPI, plugin.Name, _configDirectory, _dataDirectory);

            // Create container
            var container = new PluginContainer(plugin, loadContext, dllPath, context);
            _plugins[fileName] = container;

            // Load the plugin
            await plugin.OnLoadAsync(context);
            await plugin.OnEnableAsync();

            Logger.Info($"Plugin loaded: {plugin.Name} v{plugin.Version}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to load plugin from {dllPath}:");
            return false;
        }
    }

    /// <summary>
    /// Unload a plugin by name
    /// </summary>
    public async Task<bool> UnloadPluginAsync(string fileName)
    {
        if (!_plugins.TryRemove(fileName, out var container))
        {
            Logger.Warning($"Plugin {fileName} not found");
            return false;
        }

        try
        {
            Logger.Info($"Unloading plugin: {container.Plugin.Name}");

            await container.Plugin.OnDisableAsync();
            await container.Plugin.OnUnloadAsync();

            // Unload the assembly
            container.LoadContext.Unload();

            // Wait for GC to collect
            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (container.LoadContext.IsCollectible)
                    break;
                await Task.Delay(100);
            }

            Logger.Info($"Plugin unloaded: {container.Plugin.Name}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to unload plugin {fileName}:");
            return false;
        }
    }

    /// <summary>
    /// Reload a plugin (unload and load again)
    /// </summary>
    public async Task<bool> ReloadPluginAsync(string fileName)
    {
        if (!_plugins.TryGetValue(fileName, out var container))
        {
            Logger.Warning($"Plugin {fileName} not found");
            return false;
        }

        var dllPath = container.DllPath;
        Logger.Info($"Reloading plugin: {container.Plugin.Name}");

        if (!await UnloadPluginAsync(fileName))
            return false;

        // Wait a bit for file handles to be released
        await Task.Delay(500);

        return await LoadPluginAsync(dllPath);
    }

    /// <summary>
    /// Get all loaded plugins
    /// </summary>
    public IEnumerable<IPlugin> GetPlugins()
    {
        return _plugins.Values.Select(c => c.Plugin);
    }

    /// <summary>
    /// Enable file system watching for hot reload
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
        var fileName = Path.GetFileName(e.FullPath);
        Logger.Info($"Plugin file changed: {fileName}");
        
        // Debounce: wait a bit for file to be fully written
        Task.Run(async () =>
        {
            await Task.Delay(1000);
            await ReloadPluginAsync(fileName);
        });
    }

    private void OnPluginFileCreated(object sender, FileSystemEventArgs e)
    {
        var fileName = Path.GetFileName(e.FullPath);
        Logger.Info($"Plugin file created: {fileName}");
        
        // Debounce: wait a bit for file to be fully written
        Task.Run(async () =>
        {
            await Task.Delay(1000);
            await LoadPluginAsync(e.FullPath);
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);

        _watcher?.Dispose();

        // Unload all plugins
        foreach (var container in _plugins.Values)
        {
            try
            {
                container.Plugin.OnDisableAsync().Wait();
                container.Plugin.OnUnloadAsync().Wait();
                container.LoadContext.Unload();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error unloading plugin {container.Plugin.Name}:");
            }
        }

        _plugins.Clear();
    }
}

/// <summary>
/// Plugin load context for assembly isolation
/// </summary>
internal class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Allow sharing of SDK and Core assemblies
        if (assemblyName.Name == "PhiraMp.Plugin.SDK" || 
            assemblyName.Name == "PhiraMp.Core" ||
            assemblyName.Name?.StartsWith("System") == true ||
            assemblyName.Name?.StartsWith("Microsoft") == true ||
            assemblyName.Name?.StartsWith("netstandard") == true)
        {
            return null; // Use default context
        }

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}

/// <summary>
/// Container holding plugin instance and its load context
/// </summary>
internal class PluginContainer
{
    public IPlugin Plugin { get; }
    public PluginLoadContext LoadContext { get; }
    public string DllPath { get; }
    public PluginContext Context { get; }

    public PluginContainer(IPlugin plugin, PluginLoadContext loadContext, string dllPath, PluginContext context)
    {
        Plugin = plugin;
        LoadContext = loadContext;
        DllPath = dllPath;
        Context = context;
    }
}
