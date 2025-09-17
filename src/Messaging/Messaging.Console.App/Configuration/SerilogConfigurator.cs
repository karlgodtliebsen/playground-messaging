using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;

using ILogger = Serilog.ILogger;

namespace Messaging.Console.App.Configuration;

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

    public static ILogger CreateConsumerLogger(IConfiguration configuration, string? context = null)
    {
        var config = new LoggerConfiguration()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.FromLogContext();

        if (!string.IsNullOrEmpty(context))
        {
            config = config.Enrich.WithProperty("TechnicalContext", context);
        }

        return config.ReadFrom.Configuration(configuration)
            .CreateLogger();
    }

    public static ILogger CreateMonitoringLogger(IConfiguration configuration, string? context = null)
    {
        var config = new LoggerConfiguration()
            .Enrich.WithProperty("LogCategory", "Monitoring");
        if (!string.IsNullOrEmpty(context))
        {
            config = config.Enrich.WithProperty("TechnicalContext", context);
        }

        return config
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    }
}

