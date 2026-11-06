using System.Collections.Frozen;
using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Auth;

namespace Schuelerfirma.Api.Infrastructure.Auth;

/// <summary>
/// Reads the role permissions from <c>Authorization:RolePermissions</c>. Changes to the configuration apply without
/// a restart. An invalid change (e.g. a typo in a permission name) is ignored with an error in the log, and the last
/// valid assignment stays in force: a typo must neither lock everybody out nor crash the running application.
/// </summary>
public sealed class ConfiguredRolePermissionProvider : IRolePermissionProvider, IDisposable
{
    private static readonly IReadOnlySet<string> NoPermissions = FrozenSet<string>.Empty;

    private readonly ILogger<ConfiguredRolePermissionProvider> logger;
    private readonly IDisposable? subscription;
    private volatile FrozenDictionary<string, FrozenSet<string>> permissionsByRole;

    /// <summary>Reads the current configuration and follows its changes.</summary>
    /// <exception cref="InvalidOperationException">The configuration at startup is invalid.</exception>
    public ConfiguredRolePermissionProvider(IOptionsMonitor<RolePermissionOptions> options, ILogger<ConfiguredRolePermissionProvider> logger)
    {
        this.logger = logger;
        var initial = options.CurrentValue;
        var errors = initial.FindErrors();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"{RolePermissionOptions.SectionName}:RolePermissions is invalid: {string.Join("; ", errors)}.");
        }

        permissionsByRole = Build(initial);
        subscription = options.OnChange(Update);
    }

    /// <inheritdoc />
    public IReadOnlySet<string> GetPermissions(string role) =>
        permissionsByRole.TryGetValue(role, out var permissions) ? permissions : NoPermissions;

    /// <inheritdoc />
    public bool HasPermission(string role, string permission) => GetPermissions(role).Contains(permission);

    /// <inheritdoc />
    public void Dispose() => subscription?.Dispose();

    private void Update(RolePermissionOptions changed)
    {
        var errors = changed.FindErrors();
        if (errors.Count > 0)
        {
            logger.LogError(
                "Ignored the changed role permissions, the previous ones stay in force: {Errors}", string.Join("; ", errors));
            return;
        }

        permissionsByRole = Build(changed);
        logger.LogInformation("Role permissions reloaded");
    }

    private static FrozenDictionary<string, FrozenSet<string>> Build(RolePermissionOptions options) =>
        options.RolePermissions.ToFrozenDictionary(
            role => role.Key,
            role => (role.Value ?? []).Contains(Permissions.Wildcard)
                ? Permissions.All.ToFrozenSet(StringComparer.Ordinal)
                : (role.Value ?? []).ToFrozenSet(StringComparer.Ordinal),
            StringComparer.OrdinalIgnoreCase);
}
