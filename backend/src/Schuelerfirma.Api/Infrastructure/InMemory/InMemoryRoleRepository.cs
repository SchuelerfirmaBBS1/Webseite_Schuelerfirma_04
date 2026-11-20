using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.InMemory;

/// <summary>In-memory <see cref="IRoleRepository"/> for development and tests.</summary>
public sealed class InMemoryRoleRepository(InMemoryStore store) : IRoleRepository
{
    /// <inheritdoc />
    public Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            IReadOnlyList<Role> roles = [.. store.Roles.OrderBy(r => r.RoleId).Select(InMemoryStore.Copy)];
            return Task.FromResult(roles);
        }
    }

    /// <inheritdoc />
    public Task<Role?> GetByIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var role = store.Roles.Find(r => r.RoleId == roleId);
            return Task.FromResult(role is null ? null : InMemoryStore.Copy(role));
        }
    }

    /// <inheritdoc />
    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var role = store.Roles.Find(r => InMemoryStore.SameText(r.Name, name));
            return Task.FromResult(role is null ? null : InMemoryStore.Copy(role));
        }
    }
}
