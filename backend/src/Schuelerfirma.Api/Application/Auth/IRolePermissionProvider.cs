namespace Schuelerfirma.Api.Application.Auth;

/// <summary>
/// Answers which permissions a role has. The current implementation reads the configuration
/// (<see cref="RolePermissionOptions"/>); it can later be replaced by one that reads the database.
/// </summary>
public interface IRolePermissionProvider
{
    /// <summary>
    /// Returns the permissions of <paramref name="role"/> from the <see cref="Permissions"/> catalog, with the
    /// wildcard already expanded. Unknown roles get an empty set.
    /// </summary>
    IReadOnlySet<string> GetPermissions(string role);

    /// <summary>Whether <paramref name="role"/> has <paramref name="permission"/>.</summary>
    bool HasPermission(string role, string permission);
}
