using System.ComponentModel.DataAnnotations;

namespace Schuelerfirma.Api.Contracts.Auth;

/// <summary>Body of <c>POST /api/auth/login</c>.</summary>
/// <param name="Username">Login name.</param>
/// <param name="Password">Password.</param>
public sealed record LoginRequest(
    [Required, StringLength(256)] string Username,
    [Required, StringLength(256)] string Password);
