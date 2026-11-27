using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Auth;

/// <summary>
/// Registers password hashing, the password policy and the temporary password generator.
/// </summary>
public static class PasswordSecurityExtensions
{
    /// <summary>
    /// PBKDF2 iterations for new hashes. The OWASP Password Storage Cheat Sheet asks for 210,000 with HMAC-SHA512,
    /// which is what the framework's <see cref="PasswordHasher{TUser}"/> uses for its current hash format.
    /// </summary>
    public const int HashIterations = 210_000;

    /// <summary>
    /// Registers <see cref="IPasswordHasher{TUser}"/> for <see cref="User"/> (framework hasher without the Identity
    /// database schema), <see cref="PasswordPolicy"/> with <c>Auth:Password</c> settings, the list of common
    /// passwords and <see cref="ITemporaryPasswordGenerator"/>.
    /// </summary>
    public static IServiceCollection AddPasswordSecurity(this IServiceCollection services)
    {
        services.AddOptions<PasswordPolicyOptions>()
            .BindConfiguration(PasswordPolicyOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.Configure<PasswordHasherOptions>(options => options.IterationCount = HashIterations);

        services.TryAddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.TryAddSingleton<ICommonPasswordList, EmbeddedCommonPasswordList>();
        services.TryAddSingleton<PasswordPolicy>();
        services.TryAddSingleton<ITemporaryPasswordGenerator, TemporaryPasswordGenerator>();
        return services;
    }
}
