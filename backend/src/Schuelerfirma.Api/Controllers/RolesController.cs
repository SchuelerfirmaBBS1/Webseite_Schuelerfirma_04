using Microsoft.AspNetCore.Mvc;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Contracts.Users;
using Schuelerfirma.Api.Infrastructure.Auth;

namespace Schuelerfirma.Api.Controllers;

/// <summary>The roles and their permissions, e.g. for the role selection in the user management.</summary>
[Route("api/admin/roles")]
[HasPermission(Permissions.UsersManage)]
public class RolesController(IRoleRepository roles, IRolePermissionProvider permissions) : AdminApiController
{
    /// <summary>Returns all roles with the permissions they have according to the configuration.</summary>
    /// <response code="200">The roles.</response>
    [HttpGet]
    public async Task<IReadOnlyList<RoleResponse>> GetAll(CancellationToken cancellationToken) =>
    [
        .. (await roles.GetAllAsync(cancellationToken)).Select(role => new RoleResponse(
            role.RoleId, role.Name, [.. permissions.GetPermissions(role.Name).Order(StringComparer.Ordinal)])),
    ];
}
