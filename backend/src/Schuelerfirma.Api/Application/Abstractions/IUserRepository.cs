using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Abstractions;

/// <summary>
/// Storage for users. Returned users are detached copies: changing one has no effect until it is
/// passed to <see cref="UpdateAsync"/>. <see cref="User.Role"/> is always loaded.
/// </summary>
/// <remarks>
/// <para>
/// EF Core: unique indexes on <see cref="User.Username"/> and <see cref="User.Email"/> with a
/// case-insensitive collation (MySQL: <c>utf8mb4_0900_ai_ci</c>), foreign key to <see cref="Role"/> without
/// cascading delete. A violated unique index must be reported as <see cref="DuplicateEntryException"/>
/// (catch <c>DbUpdateException</c> and check for MySQL error 1062).
/// </para>
/// <para>
/// The counter methods (<see cref="IncrementFailedLoginCountAsync"/> and the ones after it) must each be a
/// single atomic statement, e.g. <c>ExecuteUpdateAsync(s =&gt; s.SetProperty(u =&gt; u.FailedLoginCount, u =&gt; u.FailedLoginCount + 1))</c>.
/// Reading, increasing and saving would lose counts when an attacker sends many attempts in parallel and
/// so weaken the lockout.
/// </para>
/// <para>All timestamps are passed in by the caller (UTC) so that time-dependent logic stays testable.</para>
/// </remarks>
public interface IUserRepository
{
    /// <summary>Returns the user with the given id, or <c>null</c>.</summary>
    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Returns the user with the given user name (case-insensitive), or <c>null</c>.</summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>Returns all users ordered by user name (case-insensitive).</summary>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Whether a user with this user name exists (case-insensitive).</summary>
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>Whether a user with this e-mail address exists (case-insensitive).</summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts active users whose role is one of <paramref name="roleIds"/>. Used to make sure the last
    /// administrator cannot be removed.
    /// </summary>
    Task<int> CountActiveInRolesAsync(IReadOnlyCollection<int> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a new user and sets <see cref="User.UserId"/> on <paramref name="user"/>.
    /// </summary>
    /// <exception cref="DuplicateEntryException">User name or e-mail address is already taken.</exception>
    /// <exception cref="InvalidOperationException">The role does not exist.</exception>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves <see cref="User.Email"/>, <see cref="User.RoleId"/>, <see cref="User.PasswordHash"/>,
    /// <see cref="User.IsActive"/> and <see cref="User.MustChangePassword"/>. Everything else is left alone:
    /// the user name never changes, and the lockout fields are only changed by the atomic methods, so a
    /// stale copy cannot overwrite a concurrent failed-login count.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The user does not exist.</exception>
    /// <exception cref="DuplicateEntryException">The e-mail address is already taken.</exception>
    /// <exception cref="InvalidOperationException">The role does not exist.</exception>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically increases <see cref="User.FailedLoginCount"/> by one.
    /// </summary>
    /// <returns>The new value, or 0 if the user does not exist.</returns>
    Task<int> IncrementFailedLoginCountAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks the account until <paramref name="lockoutEnd"/> and resets the failed-login count to 0, so the
    /// user gets a fresh set of attempts when the lockout is over. Does nothing if the user does not exist.
    /// </summary>
    Task LockOutAsync(int userId, DateTime lockoutEnd, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a successful login: resets the failed-login count, removes the lockout and sets
    /// <see cref="User.LastLoginAt"/>. Does nothing if the user does not exist.
    /// </summary>
    Task RecordSuccessfulLoginAsync(int userId, DateTime loginAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the failed-login count and removes the lockout without touching
    /// <see cref="User.LastLoginAt"/> (used when an admin resets a password). Does nothing if the user does not exist.
    /// </summary>
    Task ClearLockoutAsync(int userId, CancellationToken cancellationToken = default);
}
