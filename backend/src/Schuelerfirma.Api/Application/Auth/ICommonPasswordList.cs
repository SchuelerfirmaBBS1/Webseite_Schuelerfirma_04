namespace Schuelerfirma.Api.Application.Auth;

/// <summary>
/// A list of passwords that are known to be used very often.
/// </summary>
public interface ICommonPasswordList
{
    /// <summary>Whether the password is on the list (case-insensitive).</summary>
    bool Contains(string password);
}
