using Microsoft.EntityFrameworkCore;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core / MySQL implementation of <see cref="IRoleRepository"/>.
/// </summary>
public sealed class EfRoleRepository(IDbContextFactory<SchuelerfirmaDbContext> contextFactory) : IRoleRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Roles
            .AsNoTracking()
            .OrderBy(r => r.RoleId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Role?> GetByIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Roles
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.RoleId == roleId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Roles
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Name == name, cancellationToken);
    }
}
