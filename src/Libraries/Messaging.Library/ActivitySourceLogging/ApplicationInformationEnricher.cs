//namespace Messaging.Library.ActivitySourceLogging;

//public sealed class ApplicationInformationEnricher : ILogEventEnricher
//{
//    private readonly ApplicationInformation information;
//    public ApplicationInformationEnricher(ApplicationInformation opt) => information = opt;

//    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
//    {
//        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("service.name", information.ServiceName));
//        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("service.version", information.ServiceVersion));
//        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("deployment.environment", information.EnvironmentName));
//        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("host.name", information.MachineName));
//        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("program.name", information.ProgramName));

//        // Domain
//        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("mes.center.id", information.CenterId ?? ""));
//        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("mes.instance.id", information.InstanceId));
//    }
//}