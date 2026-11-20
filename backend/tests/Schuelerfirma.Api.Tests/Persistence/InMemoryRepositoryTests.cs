using Schuelerfirma.Api.Domain;
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
            new InMemoryUserSessionRepository(store),
            new InMemoryCategoryRepository(store),
            new InMemoryProductRepository(store),
            image => AddProductImage(store, image));
    }

    private static Task AddProductImage(InMemoryStore store, ProductImage image)
    {
        lock (store.Sync)
        {
            var stored = InMemoryStore.Copy(image);
            stored.ProductImageId = store.ProductImages.Count == 0 ? 1 : store.ProductImages.Max(i => i.ProductImageId) + 1;
            store.ProductImages.Add(stored);
            image.ProductImageId = stored.ProductImageId;
            return Task.CompletedTask;
        }
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

public class InMemoryCategoryRepositoryTests : CategoryRepositoryContractTests
{
    protected override Repositories CreateRepositories() => InMemoryRepositories.Create();
}

public class InMemoryProductRepositoryTests : ProductRepositoryContractTests
{
    protected override Repositories CreateRepositories() => InMemoryRepositories.Create();
}
