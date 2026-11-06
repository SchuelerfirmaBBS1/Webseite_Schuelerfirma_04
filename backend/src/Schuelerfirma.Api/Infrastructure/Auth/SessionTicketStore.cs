using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Auth;

/// <summary>
/// Keeps login sessions on the server. The cookie only carries a random session key (and is encrypted by ASP.NET
/// Core on top of that); the database only stores the SHA-256 hash of the key together with the authentication
/// ticket, so a leaked database does not allow anyone to log in.
/// </summary>
/// <remarks>
/// A session ends after <see cref="LoginSessionOptions.IdleTimeout"/> without activity and in any case
/// <see cref="LoginSessionOptions.MaxLifetime"/> after it was created; renewing a session never extends it beyond that.
/// </remarks>
public sealed class SessionTicketStore(
    IServiceScopeFactory scopes,
    TimeProvider timeProvider,
    IOptions<LoginSessionOptions> options) : ITicketStore
{
    private readonly LoginSessionOptions session = options.Value;

    /// <inheritdoc />
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var userId = ticket.Principal.GetUserId()
            ?? throw new InvalidOperationException("The ticket does not contain a user id.");

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var key = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var expiresAt = CapExpiry(now + session.IdleTimeout, createdAt: now);
        ticket.Properties.ExpiresUtc = expiresAt;

        using var scope = scopes.CreateScope();
        await Sessions(scope).AddAsync(new UserSession
        {
            SessionKeyHash = Hash(key),
            UserId = userId,
            Ticket = TicketSerializer.Default.Serialize(ticket),
            CreatedAt = now,
            ExpiresAt = expiresAt,
        });
        return key;
    }

    /// <inheritdoc />
    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        using var scope = scopes.CreateScope();
        var sessions = Sessions(scope);
        var stored = await sessions.GetAsync(Hash(key));
        if (stored is null)
        {
            return;
        }

        var expiresAt = CapExpiry(now + session.IdleTimeout, stored.CreatedAt);
        ticket.Properties.ExpiresUtc = expiresAt;
        stored.Ticket = TicketSerializer.Default.Serialize(ticket);
        stored.ExpiresAt = expiresAt;
        await sessions.UpdateAsync(stored);
    }

    /// <inheritdoc />
    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hash = Hash(key);

        using var scope = scopes.CreateScope();
        var sessions = Sessions(scope);
        var stored = await sessions.GetAsync(hash);
        if (stored is null)
        {
            return null;
        }

        if (stored.ExpiresAt <= now || stored.CreatedAt + session.MaxLifetime <= now)
        {
            await sessions.DeleteAsync(hash);
            return null;
        }

        return TicketSerializer.Default.Deserialize(stored.Ticket);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key)
    {
        using var scope = scopes.CreateScope();
        await Sessions(scope).DeleteAsync(Hash(key));
    }

    /// <summary>The value stored in the database for a session key: SHA-256 as lowercase hex.</summary>
    public static string Hash(string sessionKey) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sessionKey)));

    private DateTime CapExpiry(DateTime wanted, DateTime createdAt)
    {
        var latest = createdAt + session.MaxLifetime;
        return wanted < latest ? wanted : latest;
    }

    private static IUserSessionRepository Sessions(IServiceScope scope) =>
        scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
}
