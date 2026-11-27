using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.InMemory;

/// <summary>In-memory <see cref="IProductImageRepository"/> for development and tests.</summary>
public sealed class InMemoryProductImageRepository(InMemoryStore store) : IProductImageRepository
{
    /// <inheritdoc />
    public Task AddAsync(ProductImage image, byte[] data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(data);

        lock (store.Sync)
        {
            if (!store.Products.Exists(p => p.ProductId == image.ProductId))
            {
                throw new InvalidOperationException("Referenced product does not exist.");
            }

            var stored = InMemoryStore.Copy(image);
            stored.ProductImageId = ++store.LastProductImageId;
            if (stored.IsPrimary)
            {
                ClearPrimary(stored.ProductId);
            }

            store.ProductImages.Add(stored);
            store.ProductImageContents[stored.ProductImageId] =
                new ProductImageContent { ProductImageId = stored.ProductImageId, Data = [.. data] };
            image.ProductImageId = stored.ProductImageId;
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task<ProductImage?> GetAsync(int productId, int productImageId, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var stored = Find(productId, productImageId);
            return Task.FromResult(stored is null ? null : InMemoryStore.Copy(stored));
        }
    }

    /// <inheritdoc />
    public Task<bool> UpdateAsync(ProductImage image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);

        lock (store.Sync)
        {
            var stored = Find(image.ProductId, image.ProductImageId);
            if (stored is null)
            {
                return Task.FromResult(false);
            }

            if (image.IsPrimary)
            {
                ClearPrimary(stored.ProductId);
            }

            stored.Description = image.Description;
            stored.SortOrder = image.SortOrder;
            stored.IsPrimary = image.IsPrimary;
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(int productId, int productImageId, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var stored = Find(productId, productImageId);
            if (stored is null)
            {
                return Task.FromResult(false);
            }

            store.ProductImages.Remove(stored);
            store.ProductImageContents.Remove(productImageId);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<ImageFile?> GetFileAsync(int productId, int productImageId, CancellationToken cancellationToken = default) =>
        File(productId, productImageId, onlyVisible: false);

    /// <inheritdoc />
    public Task<ImageFile?> GetVisibleFileAsync(int productId, int productImageId, CancellationToken cancellationToken = default) =>
        File(productId, productImageId, onlyVisible: true);

    private Task<ImageFile?> File(int productId, int productImageId, bool onlyVisible)
    {
        lock (store.Sync)
        {
            var stored = Find(productId, productImageId);
            if (stored is null || !store.ProductImageContents.TryGetValue(productImageId, out var content))
            {
                return Task.FromResult<ImageFile?>(null);
            }

            if (onlyVisible && !IsVisible(productId))
            {
                return Task.FromResult<ImageFile?>(null);
            }

            return Task.FromResult<ImageFile?>(new ImageFile(stored.ContentType, [.. content.Data]));
        }
    }

    // Called inside the lock.
    private ProductImage? Find(int productId, int productImageId) =>
        store.ProductImages.Find(i => i.ProductImageId == productImageId && i.ProductId == productId);

    // Called inside the lock.
    private bool IsVisible(int productId) =>
        store.Products.Find(p => p.ProductId == productId) is { IsActive: true } product
        && store.Categories.Exists(c => c.CategoryId == product.CategoryId && c.IsVisible);

    // Called inside the lock.
    private void ClearPrimary(int productId)
    {
        foreach (var other in store.ProductImages.Where(i => i.ProductId == productId))
        {
            other.IsPrimary = false;
        }
    }
}
