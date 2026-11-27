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
            (image, data) => AddProductImage(store, image, data),
            imageId => GetProductImageData(store, imageId));
    }

    private static Task AddProductImage(InMemoryStore store, ProductImage image, byte[] data)
    {
        lock (store.Sync)
        {
            var stored = InMemoryStore.Copy(image);
            stored.ProductImageId = store.ProductImages.Count == 0 ? 1 : store.ProductImages.Max(i => i.ProductImageId) + 1;
            store.ProductImages.Add(stored);
            store.ProductImageContents[stored.ProductImageId] =
                new ProductImageContent { ProductImageId = stored.ProductImageId, Data = [.. data] };
            image.ProductImageId = stored.ProductImageId;
            return Task.CompletedTask;
        }
    }

    private static Task<byte[]?> GetProductImageData(InMemoryStore store, int imageId)
    {
        lock (store.Sync)
        {
            return Task.FromResult(
                store.ProductImageContents.TryGetValue(imageId, out var content) ? (byte[]?)[.. content.Data] : null);
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
