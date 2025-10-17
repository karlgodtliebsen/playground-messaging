using OpenTelemetry.Resources;

namespace Messaging.Observability.Library.Configuration;

public static class ApplicationInformationOtelExtensions
{
    public static ResourceBuilder ApplyToResource(this ResourceBuilder rb, ApplicationInformationOptions o)
    {
        rb = rb.AddService(o.ServiceName, serviceVersion: o.ServiceVersion, serviceInstanceId: o.ServiceInstanceId, serviceNamespace: o.ServiceNamespace);

        return rb.AddAttributes([
            new KeyValuePair<string, object>("deployment.environment", o.EnvironmentName), new KeyValuePair<string, object>("run.id", o.RunId), new KeyValuePair<string, object>("host.name", o.MachineName),
            new KeyValuePair<string, object>("host.id", o.HostId ?? o.MachineName), new KeyValuePair<string, object>("os.type", o.OsType), new KeyValuePair<string, object>("os.description", o.OsDescription),
            new KeyValuePair<string, object>("host.arch", o.HostArch), new KeyValuePair<string, object>("process.pid", o.ProcessId), new KeyValuePair<string, object>("process.executable.name", o.ProcessExecutableName),
            new KeyValuePair<string, object>("process.executable.path", o.ProcessExecutablePath ?? string.Empty), new KeyValuePair<string, object>("process.command_line", o.CommandLine),
            new KeyValuePair<string, object>("net.host.name", o.Fqdn), new KeyValuePair<string, object>("net.host.ip", string.Join(",", o.IpAddresses)), new KeyValuePair<string, object>("telemetry.sdk.language", "dotnet"),
            new KeyValuePair<string, object>("framework.description", o.FrameworkDescription), new KeyValuePair<string, object>("clr.version", o.ClrVersion), new KeyValuePair<string, object>("app.path", o.ApplicationPath),
            new KeyValuePair<string, object>("program.name", o.ProgramName),
            // Domain attrs
            //new KeyValuePair<string, object>("mes.work.center.id", o.CenterId ?? string.Empty), new KeyValuePair<string, object>("mes.work.station.id", o.InstanceId ?? string.Empty)
        ]);
    }
}