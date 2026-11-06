using Microsoft.AspNetCore.Authorization;
using Schuelerfirma.Api.Application.Auth;

namespace Schuelerfirma.Api.Infrastructure.Auth;

/// <summary>
/// Registers the configurable role permissions behind <see cref="HasPermissionAttribute"/>.
/// </summary>
public static class RolePermissionExtensions
{
    /// <summary>Binds <c>Authorization:RolePermissions</c>, the provider, the handler and the startup check.</summary>
    public static IServiceCollection AddRolePermissions(this IServiceCollection services)
    {
        // Validation happens in the provider, not with ValidateOnStart: a failed options validation would throw on
        // every access after an invalid reload, while the provider keeps the last valid assignment.
        services.AddOptions<RolePermissionOptions>().BindConfiguration(RolePermissionOptions.SectionName);
        services.AddSingleton<IRolePermissionProvider, ConfiguredRolePermissionProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddHostedService<RolePermissionStartupCheck>();
        return services;
    }
}
