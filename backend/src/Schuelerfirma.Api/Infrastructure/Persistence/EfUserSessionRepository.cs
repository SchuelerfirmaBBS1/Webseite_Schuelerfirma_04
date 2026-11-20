using Microsoft.EntityFrameworkCore;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core / MySQL implementation of <see cref="IUserSessionRepository"/>.
/// </summary>
public sealed class EfUserSessionRepository(IDbContextFactory<SchuelerfirmaDbContext> contextFactory) : IUserSessionRepository
{
    /// <inheritdoc />
    public async Task AddAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = new UserSession
        {
            SessionKeyHash = session.SessionKeyHash,
            UserId = session.UserId,
            Ticket = session.Ticket,
            CreatedAt = session.CreatedAt,
            ExpiresAt = session.ExpiresAt
        };

        db.UserSessions.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw MySqlExceptionTranslator.TranslateSessionException(ex);
        }
    }

    /// <inheritdoc />
    public async Task<UserSession?> GetAsync(string sessionKeyHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionKeyHash);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.UserSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.SessionKeyHash == sessionKeyHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        await db.UserSessions
            .Where(s => s.SessionKeyHash == session.SessionKeyHash)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(s => s.Ticket, session.Ticket)
                    .SetProperty(s => s.ExpiresAt, session.ExpiresAt),
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string sessionKeyHash, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionKeyHash);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        await db.UserSessions
            .Where(s => s.SessionKeyHash == sessionKeyHash)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteAllForUserAsync(
        int userId,
        string? exceptSessionKeyHash = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = db.UserSessions.Where(s => s.UserId == userId);

        if (exceptSessionKeyHash is not null)
        {
            query = query.Where(s => s.SessionKeyHash != exceptSessionKeyHash);
        }

        return await query.ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> DeleteExpiredAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.UserSessions
            .Where(s => s.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
