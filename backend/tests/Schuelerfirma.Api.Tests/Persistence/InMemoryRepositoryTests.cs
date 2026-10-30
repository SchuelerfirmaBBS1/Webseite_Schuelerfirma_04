using Schuelerfirma.Api.Infrastructure.InMemory;

namespace Schuelerfirma.Api.Tests.Persistence;

/// <summary>Runs the repository contract tests against the in-memory implementation.</summary>
internal static class InMemoryRepositories
{
    public static Repositories Create()
    {
        var store = new InMemoryStore();
        return new Repositories(
            new InMemoryUserRepository(store),
            new InMemoryRoleRepository(store),
            new InMemoryUserSessionRepository(store));
    }
}

public class InMemoryRoleRepositoryTests : RoleRepositoryContractTests
{
    protected override Repositories CreateRepositories() => InMemoryRepositories.Create();
}

public class InMemoryUserRepositoryTests : UserRepositoryContractTests
{
    protected override Repositories CreateRepositories() => InMemoryRepositories.Create();
}

public class InMemoryUserSessionRepositoryTests : UserSessionRepositoryContractTests
{
    protected override Repositories CreateRepositories() => InMemoryRepositories.Create();
}
