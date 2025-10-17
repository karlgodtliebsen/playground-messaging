using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Messaging.Library.Configuration;

/// <summary>
/// General Application Instance Information
/// For InstanceId: Inject  InstanceId for each specific process running, using Environment/Command args.
/// Must be configured/domain wide and well known
/// Use ApplicationInformation:InstanceId as configuration key
/// </summary>
public class ApplicationInformationOptions
{
    public const string SectionName = "ApplicationInformation";

    // Identity
    public string ServiceName { get; init; } = GetEntryAssemblyName();
    public string? ServiceNamespace { get; init; }
    public string ServiceVersion { get; init; } = GetEntryAssemblyVersion();

    public string EnvironmentName { get; init; } = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                                                   ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                                                   ?? "Production";

    // Domain identity (your world)
    public string InstanceId { get; init; } = Guid.CreateVersion7(DateTimeOffset.UtcNow).ToString("N");

    // Telemetry instance/run
    public string ServiceInstanceId { get; init; } = $"{Environment.MachineName}:{GetEntryAssemblyName()}";
    public string RunId { get; } = Guid.NewGuid().ToString("N");

    // Host / OS
    public string MachineName { get; init; } = Environment.MachineName;
    public string? HostId { get; init; } // optional stable machine GUID if you have one
    public string OsType { get; init; } = GetOsType();
    public string OsDescription { get; init; } = RuntimeInformation.OSDescription;
    public string HostArch { get; init; } = RuntimeInformation.OSArchitecture.ToString();

    public bool IsContainer { get; init; } = string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);

    // Process / runtime
    public int ProcessId { get; } = Environment.ProcessId;
    public string ProcessExecutableName { get; } = Path.GetFileName(Environment.ProcessPath ?? "");
    public string? ProcessExecutablePath { get; } = Environment.ProcessPath;
    public string CommandLine { get; } = Environment.CommandLine;
    public DateTimeOffset StartedAtUtc { get; } = Process.GetCurrentProcess().StartTime.ToUniversalTime();
    public string FrameworkDescription { get; } = RuntimeInformation.FrameworkDescription;
    public string ClrVersion { get; } = Environment.Version.ToString();

    // Paths & program label
    public string ApplicationPath { get; init; } = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
    public string ApplicationName { get; set; } = GetEntryAssemblyName();
    public string ApplicationVersion { get; init; } = GetEntryAssemblyVersion();
    public string ProgramName => $"{ApplicationName}/{ApplicationVersion}";

    // Network (lazy so we don’t block static construction)
    private string[]? ipAddresses;
    public string[] IpAddresses => ipAddresses ??= GetLocalIPv4s();
    public string Fqdn { get; init; } = GetFqdn();

    // Build / CI
    public string? BuildBranch { get; init; }
    public string? BuildCommitSha { get; init; } = GetSourceRevisionId();
    public string? BuildNumber { get; init; }
    public DateTimeOffset? BuildDateUtc { get; init; }

    // Locale / context
    public string TimeZoneId { get; init; } = TimeZoneInfo.Local.Id;
    public string Culture { get; init; } = CultureInfo.CurrentCulture.Name;
    public string HostingModel { get; init; } = "Unknown"; // WebApi | Worker | Desktop


    // Helpers
    private static string GetEntryAssemblyName() => Assembly.GetEntryAssembly()?.GetName().Name?.ToLowerInvariant() ?? "app";

    private static string GetEntryAssemblyVersion() => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";

    private static string GetOsType() =>
        OperatingSystem.IsWindows() ? "windows" :
        OperatingSystem.IsLinux() ? "linux" :
        OperatingSystem.IsMacOS() ? "darwin" : "unknown";

    private static string[] GetLocalIPv4s() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Select(ua => ua.Address)
            .Where(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a))
            .Select(a => a.ToString())
            .Distinct()
            .ToArray();

    private static string GetFqdn()
    {
        try
        {
            var host = Dns.GetHostName();
            var entry = Dns.GetHostEntry(host);
            return string.IsNullOrWhiteSpace(entry.HostName) ? host : entry.HostName;
        }
        catch { return Environment.MachineName; }
    }

    private static string? GetSourceRevisionId() =>
        Assembly.GetEntryAssembly()?
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "SourceRevisionId")?.Value;

    public static string Structure =
        """
         "ApplicationInformation": {
             "ApplicationName": "ApplicationName",
             "ApplicationPath": "ApplicationPath",
             "ApplicationVersion": "ApplicationVersion",
             "IpAddresses": "IpAddresses",
             "MachineName": "MachineName"
             "InstanceId": "Handle InstanceId for each specific process running. Must be configured/domain wide and well known. 
                            Use ApplicationInformation:InstanceId (ApplicationInformation__InstanceId) as configuration key"
         }
        """;
}