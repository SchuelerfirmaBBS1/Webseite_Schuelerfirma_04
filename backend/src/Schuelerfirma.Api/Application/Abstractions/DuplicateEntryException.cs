namespace Schuelerfirma.Api.Application.Abstractions;

/// <summary>
/// Thrown by a repository when a unique value (user name or e-mail address) is already taken.
/// A check with <c>ExistsBy…</c> before saving is not enough because two requests can run at the
/// same time; the repository is the one place that can decide this reliably.
/// </summary>
/// <param name="field">Name of the violated property, e.g. <c>Username</c>.</param>
public class DuplicateEntryException(string field)
    : Exception($"The value of '{field}' is already in use.")
{
    /// <summary>Name of the property whose value is already taken.</summary>
    public string Field { get; } = field;
}
