using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Schuelerfirma.Api.Application.Auth;

namespace Schuelerfirma.Api.Infrastructure.Auth;

/// <summary>
/// Requires a logged-in user whose role has <paramref name="permission"/>; otherwise the answer is 401 (not logged
/// in) or 403 (logged in, but not allowed). Several attributes on one endpoint must all be satisfied.
/// </summary>
/// <param name="permission">A name from <see cref="Permissions"/>, e.g. <see cref="Permissions.EventsCreate"/>.</param>
/// <exception cref="ArgumentException">The name is not in the <see cref="Permissions"/> catalog.</exception>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute, IAuthorizationRequirementData
{
    /// <summary>The required permission.</summary>
    public string Permission { get; } = Permissions.IsKnown(permission)
        ? permission
        : throw new ArgumentException($"'{permission}' is not in the permission catalog (Permissions).", nameof(permission));

    /// <inheritdoc />
    public IEnumerable<IAuthorizationRequirement> GetRequirements() => [new PermissionRequirement(Permission)];
}

/// <summary>The requirement behind <see cref="HasPermissionAttribute"/>.</summary>
/// <param name="Permission">The required permission.</param>
public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

/// <summary>
/// Grants <see cref="PermissionRequirement"/> if the user's role has the permission and the user does not have to
/// change the password first (temporary password). The claims are rebuilt from the database on every request
/// (<see cref="SessionCookieEvents"/>), so role changes and the password change apply immediately.
/// </summary>
public sealed class PermissionAuthorizationHandler(IRolePermissionProvider permissions)
    : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && !context.User.MustChangePassword()
            && context.User.FindAll(ClaimTypes.Role).Any(role => permissions.HasPermission(role.Value, requirement.Permission)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
