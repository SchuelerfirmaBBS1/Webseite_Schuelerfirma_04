using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Tests.Persistence;

/// <summary>The repositories under test; they must share one underlying store.</summary>
public sealed record Repositories(IUserRepository Users, IRoleRepository Roles, IUserSessionRepository Sessions);

/// <summary>
/// Base class of all repository contract tests. Every storage implementation (in-memory now, EF Core / MySQL
/// later) derives from the three contract classes and only provides <see cref="CreateRepositories"/>, so all
/// implementations are held to exactly the same behavior.
/// </summary>
public abstract class RepositoryContractTests
{
    /// <summary>Creates repositories on top of a fresh, empty store that contains only the seed roles.</summary>
    protected abstract Repositories CreateRepositories();

    protected static readonly DateTime Now = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    protected static int AdminRoleId => DefaultRoles.CreateSeed().Single(r => r.Name == DefaultRoles.Admin).RoleId;

    protected static int UserRoleId => DefaultRoles.CreateSeed().Single(r => r.Name == DefaultRoles.User).RoleId;

    protected static User NewUser(string username = "anna", string? email = null, int? roleId = null) => new()
    {
        Username = username,
        Email = email ?? $"{username}@example.test",
        PasswordHash = "hash",
        RoleId = roleId ?? UserRoleId,
        IsActive = true,
        CreatedAt = Now,
    };

    protected static async Task<User> AddUserAsync(Repositories repos, string username = "anna", int? roleId = null)
    {
        var user = NewUser(username, roleId: roleId);
        await repos.Users.AddAsync(user);
        return user;
    }
}
