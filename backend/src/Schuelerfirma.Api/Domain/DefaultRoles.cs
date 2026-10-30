namespace Schuelerfirma.Api.Domain;

/// <summary>
/// The roles that exist from the start. Used as seed data by the in-memory repositories and,
/// later, by the EF Core model (<c>HasData</c>), so both agree on names and ids.
/// </summary>
public static class DefaultRoles
{
    /// <summary>Name of the administrator role.</summary>
    public const string Admin = "Admin";

    /// <summary>Name of the role for regular members of the Schülerfirma.</summary>
    public const string User = "User";

    /// <summary>Creates fresh instances of the seed roles.</summary>
    public static IReadOnlyList<Role> CreateSeed() =>
    [
        new Role { RoleId = 1, Name = Admin },
        new Role { RoleId = 2, Name = User },
    ];
}
