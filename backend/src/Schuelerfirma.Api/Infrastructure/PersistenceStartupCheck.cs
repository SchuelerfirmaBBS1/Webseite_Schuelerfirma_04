using Schuelerfirma.Api.Application.Abstractions;

namespace Schuelerfirma.Api.Infrastructure;

/// <summary>
/// Makes sure the application never runs without real storage.
/// </summary>
public static class PersistenceStartupCheck
{
    /// <summary>
    /// Verifies that every repository interface has an implementation. Outside Development nothing is
    /// registered until the EF Core implementation exists (#22), so the application refuses to start
    /// instead of silently running without a database.
    /// </summary>
    /// <exception cref="InvalidOperationException">A repository is not registered.</exception>
    public static void EnsurePersistenceRegistered(this IServiceProvider services)
    {
        Type[] required = [typeof(IUserRepository), typeof(IRoleRepository), typeof(IUserSessionRepository)];

        var missing = required.Where(type => services.GetService(type) is null).Select(type => type.Name).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"No persistence is registered (missing: {string.Join(", ", missing)}). " +
                "The in-memory repositories are only available in Development.");
        }
    }
}
