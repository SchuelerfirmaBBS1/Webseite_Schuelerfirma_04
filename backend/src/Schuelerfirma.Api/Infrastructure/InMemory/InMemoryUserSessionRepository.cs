using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.InMemory;

/// <summary>In-memory <see cref="IUserSessionRepository"/> for development and tests.</summary>
public sealed class InMemoryUserSessionRepository(InMemoryStore store) : IUserSessionRepository
{
    /// <inheritdoc />
    public Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            if (!store.Users.Exists(u => u.UserId == session.UserId))
            {
                throw new InvalidOperationException($"User {session.UserId} does not exist.");
            }

            if (!store.Sessions.TryAdd(session.SessionKeyHash, InMemoryStore.Copy(session)))
            {
                throw new InvalidOperationException("A session with this key hash already exists.");
            }

            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task<UserSession?> GetAsync(string sessionKeyHash, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var found = store.Sessions.TryGetValue(sessionKeyHash, out var stored);
            return Task.FromResult(found ? InMemoryStore.Copy(stored!) : null);
        }
    }

    /// <inheritdoc />
    public Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            if (store.Sessions.TryGetValue(session.SessionKeyHash, out var stored))
            {
                stored.Ticket = (byte[])session.Ticket.Clone();
                stored.ExpiresAt = session.ExpiresAt;
            }

            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task DeleteAsync(string sessionKeyHash, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            store.Sessions.Remove(sessionKeyHash);
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task<int> DeleteAllForUserAsync(int userId, string? exceptSessionKeyHash = null, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            return Task.FromResult(RemoveWhere(s => s.UserId == userId && s.SessionKeyHash != exceptSessionKeyHash));
        }
    }

    /// <inheritdoc />
    public Task<int> DeleteExpiredAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            return Task.FromResult(RemoveWhere(s => s.ExpiresAt <= now));
        }
    }

    private int RemoveWhere(Func<UserSession, bool> match)
    {
        var keys = store.Sessions.Values.Where(match).Select(s => s.SessionKeyHash).ToList();
        keys.ForEach(key => store.Sessions.Remove(key));
        return keys.Count;
    }
}
