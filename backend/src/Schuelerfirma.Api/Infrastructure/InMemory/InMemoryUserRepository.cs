using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.InMemory;

/// <summary>In-memory <see cref="IUserRepository"/> for development and tests.</summary>
public sealed class InMemoryUserRepository(InMemoryStore store) : IUserRepository
{
    /// <inheritdoc />
    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            return Task.FromResult(Find(u => u.UserId == userId));
        }
    }

    /// <inheritdoc />
    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            return Task.FromResult(Find(u => InMemoryStore.SameText(u.Username, username)));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            IReadOnlyList<User> users =
            [
                .. store.Users
                    .OrderBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
                    .Select(store.Materialize),
            ];
            return Task.FromResult(users);
        }
    }

    /// <inheritdoc />
    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            return Task.FromResult(store.Users.Any(u => InMemoryStore.SameText(u.Username, username)));
        }
    }

    /// <inheritdoc />
    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            return Task.FromResult(store.Users.Any(u => InMemoryStore.SameText(u.Email, email)));
        }
    }

    /// <inheritdoc />
    public Task<int> CountActiveInRolesAsync(IReadOnlyCollection<int> roleIds, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            return Task.FromResult(store.Users.Count(u => u.IsActive && roleIds.Contains(u.RoleId)));
        }
    }

    /// <inheritdoc />
    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            if (store.Users.Any(u => InMemoryStore.SameText(u.Username, user.Username)))
            {
                throw new DuplicateEntryException(nameof(User.Username));
            }

            if (store.Users.Any(u => InMemoryStore.SameText(u.Email, user.Email)))
            {
                throw new DuplicateEntryException(nameof(User.Email));
            }

            EnsureRoleExists(user.RoleId);

            user.UserId = ++store.LastUserId;
            store.Users.Add(InMemoryStore.Copy(user));
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var stored = store.Users.Find(u => u.UserId == user.UserId)
                ?? throw new KeyNotFoundException($"User {user.UserId} does not exist.");

            if (store.Users.Any(u => u.UserId != user.UserId && InMemoryStore.SameText(u.Email, user.Email)))
            {
                throw new DuplicateEntryException(nameof(User.Email));
            }

            EnsureRoleExists(user.RoleId);

            // Username and the lockout fields are deliberately not copied, see the interface remarks.
            stored.Email = user.Email;
            stored.RoleId = user.RoleId;
            stored.PasswordHash = user.PasswordHash;
            stored.IsActive = user.IsActive;
            stored.MustChangePassword = user.MustChangePassword;
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task<int> IncrementFailedLoginCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var stored = store.Users.Find(u => u.UserId == userId);
            return Task.FromResult(stored is null ? 0 : ++stored.FailedLoginCount);
        }
    }

    /// <inheritdoc />
    public Task LockOutAsync(int userId, DateTime lockoutEnd, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var stored = store.Users.Find(u => u.UserId == userId);
            if (stored is not null)
            {
                stored.LockoutEnd = lockoutEnd;
                stored.FailedLoginCount = 0;
            }

            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task RecordSuccessfulLoginAsync(int userId, DateTime loginAt, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var stored = store.Users.Find(u => u.UserId == userId);
            if (stored is not null)
            {
                stored.FailedLoginCount = 0;
                stored.LockoutEnd = null;
                stored.LastLoginAt = loginAt;
            }

            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task ClearLockoutAsync(int userId, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var stored = store.Users.Find(u => u.UserId == userId);
            if (stored is not null)
            {
                stored.FailedLoginCount = 0;
                stored.LockoutEnd = null;
            }

            return Task.CompletedTask;
        }
    }

    private User? Find(Predicate<User> match)
    {
        var stored = store.Users.Find(match);
        return stored is null ? null : store.Materialize(stored);
    }

    private void EnsureRoleExists(int roleId)
    {
        if (!store.Roles.Exists(r => r.RoleId == roleId))
        {
            throw new InvalidOperationException($"Role {roleId} does not exist.");
        }
    }
}
