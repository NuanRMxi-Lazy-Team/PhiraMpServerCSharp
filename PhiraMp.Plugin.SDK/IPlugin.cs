namespace PhiraMp.Plugin.SDK;

/// <summary>
/// Plugin metadata attribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PluginAttribute : Attribute
{
    public string Name { get; }
    public string Version { get; }
    public string Author { get; }
    public string Description { get; }

    public PluginAttribute(string name, string version, string author = "", string description = "")
    {
        Name = name;
        Version = version;
        Author = author;
        Description = description;
    }
}

/// <summary>
/// Base interface for all plugins
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Plugin name
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plugin version
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Called when plugin is loaded
    /// </summary>
    Task OnLoadAsync(IPluginContext context);

    /// <summary>
    /// Called when plugin is unloaded
    /// </summary>
    Task OnUnloadAsync();

    /// <summary>
    /// Called when plugin is enabled
    /// </summary>
    Task OnEnableAsync();

    /// <summary>
    /// Called when plugin is disabled
    /// </summary>
    Task OnDisableAsync();
}

/// <summary>
/// Base plugin class with default implementations
/// </summary>
public abstract class PluginBase : IPlugin
{
    protected IPluginContext Context { get; private set; } = null!;

    public abstract string Name { get; }
    public abstract string Version { get; }

    public virtual Task OnLoadAsync(IPluginContext context)
    {
        Context = context;
        return Task.CompletedTask;
    }

    public virtual Task OnUnloadAsync()
    {
        return Task.CompletedTask;
    }

    public virtual Task OnEnableAsync()
    {
        return Task.CompletedTask;
    }

    public virtual Task OnDisableAsync()
    {
        return Task.CompletedTask;
    }
}
