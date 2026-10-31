using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Abstractions;

/// <summary>
/// Storage for server-side login sessions. Returned sessions are detached copies;
/// <see cref="UserSession.User"/> is not loaded.
/// </summary>
/// <remarks>
/// EF Core: <see cref="UserSession.SessionKeyHash"/> as primary key (fixed length, ASCII), foreign key to
/// <see cref="User"/>, index on <see cref="UserSession.UserId"/> and on <see cref="UserSession.ExpiresAt"/>
/// (for <see cref="DeleteAllForUserAsync"/> and <see cref="DeleteExpiredAsync"/>). Use
/// <c>ExecuteDeleteAsync</c> for the bulk deletes.
/// </remarks>
public interface IUserSessionRepository
{
    /// <summary>Saves a new session.</summary>
    /// <exception cref="InvalidOperationException">The key hash already exists or the user does not exist.</exception>
    Task AddAsync(UserSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the session with the given key hash, or <c>null</c>. Expired sessions are returned as well;
    /// the caller checks <see cref="UserSession.ExpiresAt"/>.
    /// </summary>
    Task<UserSession?> GetAsync(string sessionKeyHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves <see cref="UserSession.Ticket"/> and <see cref="UserSession.ExpiresAt"/> of an existing session
    /// (sliding expiration, ticket renewal). Does nothing if the session no longer exists, so a session that
    /// was revoked in the meantime is not brought back.
    /// </summary>
    Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default);

    /// <summary>Deletes a session (logout). Does nothing if it does not exist.</summary>
    Task DeleteAsync(string sessionKeyHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all sessions of a user, optionally keeping one (e.g. the current one after a password change).
    /// </summary>
    /// <returns>The number of deleted sessions.</returns>
    Task<int> DeleteAllForUserAsync(int userId, string? exceptSessionKeyHash = null, CancellationToken cancellationToken = default);

    /// <summary>Deletes all sessions whose <see cref="UserSession.ExpiresAt"/> is <paramref name="now"/> or earlier.</summary>
    /// <returns>The number of deleted sessions.</returns>
    Task<int> DeleteExpiredAsync(DateTime now, CancellationToken cancellationToken = default);
}
