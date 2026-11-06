using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Auth;

/// <summary>
/// Creates the first administrator at startup if there is none, so a fresh installation can be logged into.
/// The credentials come from the <c>Bootstrap</c> configuration (<c>dotnet user-secrets</c> or environment
/// variables). The account must change its password at the first login.
/// </summary>
public sealed class AdminBootstrapper(
    IServiceScopeFactory scopes,
    IOptions<BootstrapOptions> options,
    TimeProvider timeProvider,
    ILogger<AdminBootstrapper> logger) : IHostedService
{
    /// <summary>
    /// Creates the administrator if no active one exists.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The configured credentials are invalid (user name, e-mail address or a password that breaks the policy).
    /// The application then refuses to start rather than running with a weak first account.
    /// </exception>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        var services = scope.ServiceProvider;
        var users = services.GetRequiredService<IUserRepository>();
        var roles = services.GetRequiredService<IRoleRepository>();

        var adminRole = await roles.GetByNameAsync(DefaultRoles.Admin, cancellationToken)
            ?? throw new InvalidOperationException($"The role '{DefaultRoles.Admin}' does not exist.");

        if (await users.CountActiveInRolesAsync([adminRole.RoleId], cancellationToken) > 0)
        {
            return;
        }

        var bootstrap = options.Value;
        if (string.IsNullOrWhiteSpace(bootstrap.AdminUsername)
            || string.IsNullOrWhiteSpace(bootstrap.AdminEmail)
            || string.IsNullOrEmpty(bootstrap.AdminPassword))
        {
            logger.LogWarning(
                "There is no active administrator and Bootstrap:AdminUsername, Bootstrap:AdminEmail and "
                + "Bootstrap:AdminPassword are not all set, so nobody can log in to the admin area. "
                + "Set them with 'dotnet user-secrets' or environment variables.");
            return;
        }

        Validate(bootstrap, services.GetRequiredService<PasswordPolicy>());

        var hasher = services.GetRequiredService<IPasswordHasher<User>>();
        var admin = new User
        {
            Username = bootstrap.AdminUsername,
            Email = bootstrap.AdminEmail,
            RoleId = adminRole.RoleId,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
        };
        admin.PasswordHash = hasher.HashPassword(admin, bootstrap.AdminPassword);

        try
        {
            await users.AddAsync(admin, cancellationToken);
        }
        catch (DuplicateEntryException exception)
        {
            // Another instance was faster, or a non-admin or deactivated user already has this name or address.
            logger.LogError(
                "The first administrator could not be created: {Field} is already used by another account",
                exception.Field);
            return;
        }

        logger.LogWarning(
            "Created the first administrator '{Username}'. The password has to be changed at the first login; "
            + "remove Bootstrap:AdminPassword from the configuration now.",
            LogText.Safe(admin.Username));
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void Validate(BootstrapOptions bootstrap, PasswordPolicy policy)
    {
        if (!AccountRules.IsValidUsername(bootstrap.AdminUsername))
        {
            throw new InvalidOperationException(
                $"Bootstrap:AdminUsername is invalid: {AccountRules.UsernameMinLength} to {User.UsernameMaxLength} characters "
                + "out of a-z, 0-9, '.', '_' and '-'.");
        }

        if (!AccountRules.IsValidEmail(bootstrap.AdminEmail))
        {
            throw new InvalidOperationException("Bootstrap:AdminEmail is not a valid e-mail address.");
        }

        var violations = policy.Validate(bootstrap.AdminPassword, bootstrap.AdminUsername);
        if (violations.Count > 0)
        {
            // Never include the password itself in the message.
            throw new InvalidOperationException(
                "Bootstrap:AdminPassword does not satisfy the password policy: "
                + string.Join(" ", violations.Select(v => v.Message)));
        }
    }
}
