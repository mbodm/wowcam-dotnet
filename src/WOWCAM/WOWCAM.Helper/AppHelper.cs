using System.Reflection;

namespace WOWCAM.Helper;

public sealed class AppHelper
{
    public static string GetApplicationName() =>
        Assembly.GetEntryAssembly()?.GetName()?.Name ?? "UNKNOWN";

    // It's the counterpart of the "Version" entry, declared in the .csproj file.
    // See Edi Wang's page -> https://edi.wang/post/2018/9/27/get-app-version-net-core
    public static string GetApplicationVersion() =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

    // See Microsoft's advice -> https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview?tabs=cli#api-incompatibility
    public static string GetApplicationExecutableFolder() =>
        Path.GetFullPath(AppContext.BaseDirectory);

    public static string GetApplicationExecutableFileName() =>
        Path.ChangeExtension(GetApplicationName(), ".exe");

    public static string GetApplicationExecutableFilePath() =>
        Path.Combine(GetApplicationExecutableFolder(), GetApplicationExecutableFileName());
}
