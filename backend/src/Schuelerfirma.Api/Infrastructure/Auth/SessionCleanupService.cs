using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Auth;

namespace Schuelerfirma.Api.Infrastructure.Auth;

/// <summary>
/// Regularly removes expired sessions, so the session table does not grow forever.
/// </summary>
public sealed class SessionCleanupService(
    IServiceScopeFactory scopes,
    TimeProvider timeProvider,
    IOptions<LoginSessionOptions> options,
    ILogger<SessionCleanupService> logger) : BackgroundService
{
    /// <summary>Deletes all sessions that have expired by now.</summary>
    /// <returns>The number of deleted sessions.</returns>
    public async Task<int> CleanupAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopes.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
        var deleted = await sessions.DeleteExpiredAsync(timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        if (deleted > 0)
        {
            logger.LogInformation("Removed {Count} expired sessions", deleted);
        }

        return deleted;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.CleanupInterval, timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A failing cleanup must not stop the loop; the next run tries again.
                logger.LogError(exception, "Removing expired sessions failed");
            }
        }
    }
}
