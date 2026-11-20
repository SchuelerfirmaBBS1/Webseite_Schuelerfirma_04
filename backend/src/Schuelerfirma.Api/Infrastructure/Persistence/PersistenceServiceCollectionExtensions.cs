using Microsoft.EntityFrameworkCore;
using Schuelerfirma.Api.Application.Abstractions;

namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>
/// Registers the MySQL / EF Core persistence services in the dependency injection container.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="SchuelerfirmaDbContext"/> and the EF Core repository implementations.
    /// </summary>
    public static IServiceCollection AddMySqlPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContextFactory<SchuelerfirmaDbContext>(options =>
            options.UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 4, 0))));

        services.AddScoped<IRoleRepository, EfRoleRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IUserSessionRepository, EfUserSessionRepository>();

        return services;
    }
}
