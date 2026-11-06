namespace Schuelerfirma.Api.Contracts.Auth;

/// <summary>The logged-in user, returned by <c>GET /api/auth/me</c>.</summary>
/// <param name="UserId">Id of the user.</param>
/// <param name="Username">Login name.</param>
/// <param name="Email">E-mail address.</param>
/// <param name="Role">Name of the user's role.</param>
/// <param name="Permissions">
/// Permissions of the role in alphabetical order, with the wildcard expanded. The frontend uses them to show or hide
/// buttons; the API checks them anyway.
/// </param>
public sealed record MeResponse(int UserId, string Username, string Email, string Role, IReadOnlyList<string> Permissions);
