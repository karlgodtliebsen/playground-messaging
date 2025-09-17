using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;

namespace Messaging.Kafka.WebApi.Configuration;

public static class SerilogConfigurator
{
    public static void AddSerilog(this IServiceCollection services, ILoggingBuilder loggingBuilder, IConfiguration configuration, Action<IServiceCollection, ILoggingBuilder, IConfiguration>? optionsAction = null)
    {
        services.AddSerilog();
        loggingBuilder.ClearProviders();
        loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
        loggingBuilder.AddSerilog();
        optionsAction?.Invoke(services, loggingBuilder, configuration);
    }

    public static Serilog.ILogger SetupSerilog(this IServiceProvider serviceProvider, LogEventLevel level = LogEventLevel.Information)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = new LoggerConfiguration()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.FromLogContext()
            .Enrich.WithSpan()
            .ReadFrom.Configuration(configuration).CreateLogger();
        Log.Logger = logger;
        return logger;
    }
}

