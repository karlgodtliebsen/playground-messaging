using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;

namespace Messaging.Console.App.Configuration;

public static class SerilogConfigurator
{
    public static void AddSerilogServices(this IServiceCollection services, ILoggingBuilder loggingBuilder, IConfiguration configuration, Action<IServiceCollection, ILoggingBuilder, IConfiguration>? optionsAction = null)
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
        loggingBuilder.AddSerilog();
        services.AddSerilog();
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

