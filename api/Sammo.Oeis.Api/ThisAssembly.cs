using System.Reflection;

static partial class ThisAssembly
{
    public static readonly Assembly Assembly = typeof(ThisAssembly).Assembly;

    public static string Name =>
        Assembly.GetName().Name!;

    public static string Title =>
        Assembly.GetCustomAttribute<AssemblyTitleAttribute>()!.Title;

    public static string Version =>
        'v' + Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

    public static string Description =>
        Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()!.Description;

    public static DirectoryInfo Directory =>
        new DirectoryInfo(Path.GetDirectoryName(Assembly.Location)!);
}
