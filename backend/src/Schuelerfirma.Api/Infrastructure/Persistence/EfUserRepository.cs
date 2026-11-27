using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core / MySQL implementation of <see cref="IUserRepository"/>.
/// </summary>
public sealed class EfUserRepository(IDbContextFactory<SchuelerfirmaDbContext> contextFactory) : IUserRepository
{
    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(username);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(username);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Users
            .AnyAsync(u => u.Username == username, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Users
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountActiveInRolesAsync(IReadOnlyCollection<int> roleIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roleIds);

        if (roleIds.Count == 0)
        {
            return 0;
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Users
            .CountAsync(u => u.IsActive && roleIds.Contains(u.RoleId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        // Copy without Role navigation so EF does not attempt to insert the role.
        var entity = new User
        {
            RoleId = user.RoleId,
            Username = user.Username,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            IsActive = user.IsActive,
            FailedLoginCount = user.FailedLoginCount,
            LockoutEnd = user.LockoutEnd,
            MustChangePassword = user.MustChangePassword,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        db.Users.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            user.UserId = entity.UserId;
        }
        catch (DbUpdateException ex)
        {
            throw MySqlExceptionTranslator.TranslateUserException(ex);
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var updated = await db.Users
                .Where(u => u.UserId == user.UserId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(u => u.Email, user.Email)
                        .SetProperty(u => u.RoleId, user.RoleId)
                        .SetProperty(u => u.PasswordHash, user.PasswordHash)
                        .SetProperty(u => u.IsActive, user.IsActive)
                        .SetProperty(u => u.MustChangePassword, user.MustChangePassword),
                    cancellationToken);

            if (updated == 0)
            {
                throw new KeyNotFoundException($"User with ID {user.UserId} was not found.");
            }
        }
        catch (MySqlException ex)
        {
            throw MySqlExceptionTranslator.TranslateUserException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> IncrementFailedLoginCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var updated = await db.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(u => u.FailedLoginCount, u => u.FailedLoginCount + 1),
                cancellationToken);

        if (updated == 0)
        {
            return 0;
        }

        var count = await db.Users
            .Where(u => u.UserId == userId)
            .Select(u => u.FailedLoginCount)
            .SingleAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return count;
    }

    /// <inheritdoc />
    public async Task LockOutAsync(int userId, DateTime lockoutEnd, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        await db.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.LockoutEnd, lockoutEnd)
                    .SetProperty(u => u.FailedLoginCount, 0),
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task RecordSuccessfulLoginAsync(int userId, DateTime loginAt, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        await db.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.FailedLoginCount, 0)
                    .SetProperty(u => u.LockoutEnd, (DateTime?)null)
                    .SetProperty(u => u.LastLoginAt, loginAt),
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task ClearLockoutAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        await db.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.FailedLoginCount, 0)
                    .SetProperty(u => u.LockoutEnd, (DateTime?)null),
                cancellationToken);
    }
}
