using MemoryMapped.Forwarder.Configuration;
using MemoryMapped.Forwarder.Repositories;
using MemoryMapped.Repository.MsSql.Repositories;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace MemoryMapped.Repository.MsSql.Configuration;

public static class RepositoryConfigurator
{
    public static IServiceCollection AddMsSqlServices(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(DatabaseConnectionOptions.SectionName + "_MsSql").Get<DatabaseConnectionOptions>();
        if (options is null) throw new ArgumentNullException(nameof(options), DatabaseConnectionOptions.SectionName);
        services.TryAddSingleton(Options.Create(options));
        services.TryAddTransient<IMessageRepository, MsSqlMessageRepository>();
        return services;
    }
}