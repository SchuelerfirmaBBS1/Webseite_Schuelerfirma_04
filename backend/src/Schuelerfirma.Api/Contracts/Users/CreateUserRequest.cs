using System.ComponentModel.DataAnnotations;

namespace Schuelerfirma.Api.Contracts.Users;

/// <summary>Body of <c>POST /api/admin/users</c>.</summary>
/// <param name="Username">3–32 characters out of <c>a-z 0-9 . _ -</c>; cannot be changed later.</param>
/// <param name="Email">E-mail address.</param>
/// <param name="RoleId">Id of the role (see <c>GET /api/admin/roles</c>).</param>
public sealed record CreateUserRequest(
    [Required, StringLength(64)] string Username,
    [Required, StringLength(320)] string Email,
    [Required] int? RoleId);
