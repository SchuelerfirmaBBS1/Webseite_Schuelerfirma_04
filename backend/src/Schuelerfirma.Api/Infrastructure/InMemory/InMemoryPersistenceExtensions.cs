using Microsoft.Extensions.DependencyInjection.Extensions;
using Schuelerfirma.Api.Application.Abstractions;

namespace Schuelerfirma.Api.Infrastructure.InMemory;

/// <summary>
/// Registers the in-memory repositories.
/// </summary>
public static class InMemoryPersistenceExtensions
{
    /// <summary>
    /// Registers one shared <see cref="InMemoryStore"/> and the repositories on top of it. The data is lost on
    /// every restart and exists only in this process, so this is for development and tests; <c>Program</c> calls
    /// it only in Development. Safe to call more than once.
    /// </summary>
    public static IServiceCollection AddInMemoryPersistence(this IServiceCollection services)
    {
        services.TryAddSingleton<InMemoryStore>();
        services.TryAddSingleton<IUserRepository, InMemoryUserRepository>();
        services.TryAddSingleton<IRoleRepository, InMemoryRoleRepository>();
        services.TryAddSingleton<IUserSessionRepository, InMemoryUserSessionRepository>();
        return services;
    }
}
