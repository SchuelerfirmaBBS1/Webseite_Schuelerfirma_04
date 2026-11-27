namespace Schuelerfirma.Api.Domain;

/// <summary>
/// A login session that is stored on the server. The cookie only contains a random key; the
/// database only contains the hash of that key, so a leaked database cannot be used to log in.
/// All timestamps are UTC.
/// </summary>
public class UserSession
{
    /// <summary>Length of <see cref="SessionKeyHash"/>: SHA-256 as lowercase hex.</summary>
    public const int SessionKeyHashLength = 64;

    /// <summary>Primary key: SHA-256 of the session key as lowercase hex.</summary>
    public string SessionKeyHash { get; set; } = string.Empty;

    /// <summary>Foreign key to <see cref="User"/>.</summary>
    public int UserId { get; set; }

    /// <summary>The session's user. Not loaded by the repository.</summary>
    public User? User { get; set; }

    /// <summary>The serialized authentication ticket (identity and cookie properties).</summary>
    public byte[] Ticket { get; set; } = [];

    /// <summary>When the session was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the session expires; extended on activity (sliding), but never beyond the maximum lifetime.</summary>
    public DateTime ExpiresAt { get; set; }
}
