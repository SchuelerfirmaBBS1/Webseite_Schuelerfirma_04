namespace Schuelerfirma.Api.Application.Auth;

/// <summary>
/// Creates the one-time passwords that admins hand to new users and that must be changed at the first login.
/// </summary>
public interface ITemporaryPasswordGenerator
{
    /// <summary>
    /// Creates a random password that satisfies the <see cref="PasswordPolicy"/> for this user.
    /// </summary>
    /// <param name="username">The user the password is for; it will not be contained in the result.</param>
    string Generate(string username);
}
