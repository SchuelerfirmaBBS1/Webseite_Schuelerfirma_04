using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Auth;

/// <summary>Why a login attempt ended the way it did. For logs and tests only, it must never reach the client.</summary>
public enum LoginOutcome
{
    /// <summary>The credentials were correct.</summary>
    Success,

    /// <summary>No user with this name exists.</summary>
    UnknownUser,

    /// <summary>The password was wrong.</summary>
    WrongPassword,

    /// <summary>The account is locked after too many failed attempts.</summary>
    LockedOut,

    /// <summary>The account is deactivated.</summary>
    Inactive,
}

/// <summary>Result of <see cref="AuthService.LoginAsync"/>.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="User">The user on success, otherwise <c>null</c>.</param>
public sealed record LoginResult(LoginOutcome Outcome, User? User = null)
{
    /// <summary>Whether the login succeeded.</summary>
    public bool Succeeded => Outcome == LoginOutcome.Success;
}
