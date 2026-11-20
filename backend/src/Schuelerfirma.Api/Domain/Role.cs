namespace Schuelerfirma.Api.Domain;

/// <summary>
/// A role that is assigned to users. What a role may do is not stored here but configured under
/// <c>Authorization:RolePermissions</c>.
/// </summary>
public class Role
{
    /// <summary>Maximum length of <see cref="Name"/>.</summary>
    public const int NameMaxLength = 50;

    /// <summary>Primary key.</summary>
    public int RoleId { get; set; }

    /// <summary>Unique role name (compared case-insensitively), e.g. <c>Admin</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Users that have this role.</summary>
    public ICollection<User> Users { get; set; } = [];
}
