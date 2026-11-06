using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Auth;

namespace Schuelerfirma.Api.Infrastructure.Auth;

/// <summary>
/// Checks the role permissions at startup: an unknown permission name (typo) stops the application, a configured
/// role that does not exist in the database is reported as a warning (its entry has no effect).
/// </summary>
public sealed class RolePermissionStartupCheck(
    IRolePermissionProvider permissions,
    IOptions<RolePermissionOptions> options,
    IServiceScopeFactory scopes,
    ILogger<RolePermissionStartupCheck> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Resolving the provider (constructor parameter) has already validated the permission names.
        _ = permissions;

        using var scope = scopes.CreateScope();
        var roles = await scope.ServiceProvider.GetRequiredService<IRoleRepository>().GetAllAsync(cancellationToken);
        var existing = roles.Select(role => role.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var configured in options.Value.RolePermissions.Keys.Where(name => !existing.Contains(name)))
        {
            logger.LogWarning(
                "{Section}:RolePermissions contains the role '{Role}', which does not exist; its permissions have no effect",
                RolePermissionOptions.SectionName,
                LogText.Safe(configured));
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
