using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.InMemory;

/// <summary>In-memory <see cref="IProductRepository"/> for development and tests.</summary>
public sealed class InMemoryProductRepository(InMemoryStore store) : IProductRepository
{
    /// <inheritdoc />
    public Task<IReadOnlyList<Product>> GetAllAsync(int? categoryId = null, CancellationToken cancellationToken = default) =>
        Query(p => categoryId is null || p.CategoryId == categoryId);

    /// <inheritdoc />
    public Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default) =>
        Find(p => p.ProductId == productId);

    /// <inheritdoc />
    public Task<IReadOnlyList<Product>> GetVisibleProductsAsync(int? categoryId = null, CancellationToken cancellationToken = default) =>
        Query(p => IsVisible(p) && (categoryId is null || p.CategoryId == categoryId));

    /// <inheritdoc />
    public Task<Product?> GetVisibleProductByIdAsync(int productId, CancellationToken cancellationToken = default) =>
        Find(p => p.ProductId == productId && IsVisible(p));

    /// <inheritdoc />
    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            EnsureCategoryExists(product.CategoryId);

            var stored = CopyFields(product, new Product { ProductId = ++store.LastProductId });
            store.Products.Add(stored);
            product.ProductId = stored.ProductId;
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var stored = store.Products.Find(p => p.ProductId == product.ProductId)
                ?? throw new KeyNotFoundException($"Product with ID {product.ProductId} was not found.");
            EnsureCategoryExists(product.CategoryId);

            CopyFields(product, stored);
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task<bool> SetPriceVisibilityAsync(int productId, bool isPriceVisible, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var stored = store.Products.Find(p => p.ProductId == productId);
            if (stored is null)
            {
                return Task.FromResult(false);
            }

            stored.IsPriceVisible = isPriceVisible;
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(int productId, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            if (store.Products.RemoveAll(p => p.ProductId == productId) == 0)
            {
                return Task.FromResult(false);
            }

            // Deleting the product deletes its images; nothing else uses them.
            foreach (var imageId in store.ProductImages.Where(p => p.ProductId == productId).Select(p => p.ImageId).ToList())
            {
                store.RemoveImage(imageId);
            }
            return Task.FromResult(true);
        }
    }

    // Called inside the lock.
    private bool IsVisible(Product product) =>
        product.IsActive && store.Categories.Exists(c => c.CategoryId == product.CategoryId && c.IsVisible);

    private Task<IReadOnlyList<Product>> Query(Func<Product, bool> filter)
    {
        lock (store.Sync)
        {
            IReadOnlyList<Product> products =
            [
                .. store.Products
                    .Where(filter)
                    .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(p => p.ProductId)
                    .Select(store.Materialize),
            ];
            return Task.FromResult(products);
        }
    }

    private Task<Product?> Find(Predicate<Product> match)
    {
        lock (store.Sync)
        {
            var product = store.Products.Find(match);
            return Task.FromResult(product is null ? null : store.Materialize(product));
        }
    }

    private void EnsureCategoryExists(int categoryId)
    {
        if (!store.Categories.Exists(c => c.CategoryId == categoryId))
        {
            throw new InvalidOperationException("Referenced category does not exist.");
        }
    }

    private static Product CopyFields(Product source, Product target)
    {
        target.CategoryId = source.CategoryId;
        target.Name = source.Name;
        target.Description = source.Description;
        target.Price = source.Price;
        target.IsPriceVisible = source.IsPriceVisible;
        target.IsActive = source.IsActive;
        return target;
    }
}
