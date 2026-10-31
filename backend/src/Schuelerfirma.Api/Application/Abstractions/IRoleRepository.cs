using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Abstractions;

/// <summary>
/// Read access to roles. Roles are seed data (see <see cref="DefaultRoles"/>), they are not edited at runtime.
/// </summary>
/// <remarks>
/// EF Core: seed <see cref="DefaultRoles.CreateSeed"/> with <c>HasData</c>, create a unique index on
/// <see cref="Role.Name"/>.
/// </remarks>
public interface IRoleRepository
{
    /// <summary>Returns all roles ordered by <see cref="Role.RoleId"/>.</summary>
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the role with the given id, or <c>null</c>.</summary>
    Task<Role?> GetByIdAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>Returns the role with the given name (case-insensitive), or <c>null</c>.</summary>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
