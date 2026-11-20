namespace Schuelerfirma.Api.Application.Auth;

/// <summary>
/// Which role has which permissions, bound from <c>Authorization:RolePermissions</c>:
/// <code>
/// "Authorization": { "RolePermissions": { "Admin": [ "*" ], "User": [ "events.create" ] } }
/// </code>
/// Role names are compared case-insensitively. A role without entry has no permissions.
/// </summary>
public class RolePermissionOptions
{
    /// <summary>Configuration section these options are bound from.</summary>
    public const string SectionName = "Authorization";

    /// <summary>Permissions per role name; <see cref="Permissions.Wildcard"/> grants every permission.</summary>
    public Dictionary<string, string[]> RolePermissions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns a description of every entry that is not in the <see cref="Permissions"/> catalog (typos), or an empty
    /// list if the configuration is valid.
    /// </summary>
    public IReadOnlyList<string> FindErrors() =>
    [
        .. RolePermissions
            .Where(role => string.IsNullOrWhiteSpace(role.Key))
            .Select(_ => "a role without name"),
        .. RolePermissions
            .SelectMany(role => (role.Value ?? []).Select(permission => (Role: role.Key, Permission: permission)))
            .Where(entry => entry.Permission != Permissions.Wildcard && !Permissions.IsKnown(entry.Permission))
            .Select(entry => $"unknown permission '{entry.Permission}' for role '{entry.Role}'"),
    ];
}
