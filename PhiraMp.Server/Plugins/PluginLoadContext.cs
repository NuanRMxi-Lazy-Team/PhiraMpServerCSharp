using System.Reflection;
using System.Runtime.Loader;

namespace PhiraMp.Server.Plugins;

/// <summary>
/// 插件专属 AssemblyLoadContext。
///
/// 加载决策树：
///   1. 宿主契约程序集（PhiraMp.Server/Core、MEF、YamlDotNet）→ 返回 null
///      → 运行时从默认 ALC 解析，确保 IPluginModule 等接口类型全局唯一，MEF 才能正确匹配。
///   2. 插件私有 NuGet 依赖 → 通过 AssemblyDependencyResolver 从插件子目录加载。
///      → deps.json 由 EnableDynamicLoading=true 自动生成，精确记录所有传递依赖的路径。
///   3. ASP.NET Core / .NET 共享框架程序集 → 返回 null
///      → 运行时从 .NET 安装目录（shared framework）解析，不需要也不应该 copy-local。
/// </summary>
public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>
    /// 始终由宿主提供的程序集。
    /// 即使插件输出目录里误包含了这些 DLL，也会被忽略，强制使用宿主版本。
    /// </summary>
    private static readonly HashSet<string> HostProvidedAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        "PhiraMp.Server",
        "PhiraMp.Core",
        "System.ComponentModel.Composition",
        "YamlDotNet",
    };

    /// <param name="pluginMainDllPath">插件主 DLL 的绝对路径。同目录应存在同名 .deps.json。</param>
    public PluginLoadContext(string pluginMainDllPath)
        : base(name: Path.GetFileNameWithoutExtension(pluginMainDllPath), isCollectible: true)
    {
        // AssemblyDependencyResolver 读取同目录下的 *.deps.json，
        // 能精确解析每一个传递依赖的绝对路径。
        _resolver = new AssemblyDependencyResolver(pluginMainDllPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // 宿主契约程序集 → 回退到默认 ALC
        if (HostProvidedAssemblies.Contains(assemblyName.Name ?? string.Empty))
            return null;

        // 尝试从 deps.json 解析精确路径（插件私有 NuGet 依赖走这里）
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
            return LoadFromAssemblyPath(assemblyPath);

        // 找不到 → 回退默认 ALC（.NET / ASP.NET Core 共享框架程序集走这里）
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
    }
}

