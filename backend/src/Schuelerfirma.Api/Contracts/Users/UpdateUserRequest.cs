using System.ComponentModel.DataAnnotations;

namespace Schuelerfirma.Api.Contracts.Users;

/// <summary>Body of <c>PUT /api/admin/users/{id}</c>; all fields are replaced.</summary>
/// <param name="Email">E-mail address.</param>
/// <param name="RoleId">Id of the role; a change ends the user's sessions.</param>
/// <param name="IsActive">Whether the user can log in; deactivating ends the user's sessions.</param>
public sealed record UpdateUserRequest(
    [Required, StringLength(320)] string Email,
    [Required] int? RoleId,
    [Required] bool? IsActive);
