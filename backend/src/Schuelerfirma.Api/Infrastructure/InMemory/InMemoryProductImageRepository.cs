using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.InMemory;

/// <summary>In-memory <see cref="IProductImageRepository"/> for development and tests.</summary>
public sealed class InMemoryProductImageRepository(InMemoryStore store) : IProductImageRepository
{
    /// <inheritdoc />
    public Task AddAsync(ProductImage assignment, Image image, byte[] data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(data);

        lock (store.Sync)
        {
            if (!store.Products.Exists(p => p.ProductId == assignment.ProductId))
            {
                throw new InvalidOperationException("Referenced product does not exist.");
            }

            var storedImage = InMemoryStore.Copy(image);
            storedImage.ImageId = ++store.LastImageId;
            var storedAssignment = InMemoryStore.Copy(assignment);
            storedAssignment.ImageId = storedImage.ImageId;
            if (storedAssignment.IsPrimary)
            {
                ClearPrimary(storedAssignment.ProductId);
            }

            store.Images.Add(storedImage);
            store.ImageContents[storedImage.ImageId] = new ImageContent { ImageId = storedImage.ImageId, Data = [.. data] };
            store.ProductImages.Add(storedAssignment);

            image.ImageId = storedImage.ImageId;
            assignment.ImageId = storedImage.ImageId;
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task<ProductImage?> GetAsync(int productId, int imageId, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var stored = Find(productId, imageId);
            return Task.FromResult(stored is null ? null : store.Materialize(stored));
        }
    }

    /// <inheritdoc />
    public Task<bool> UpdateAsync(ProductImage assignment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        lock (store.Sync)
        {
            var stored = Find(assignment.ProductId, assignment.ImageId);
            if (stored is null)
            {
                return Task.FromResult(false);
            }

            if (assignment.IsPrimary)
            {
                ClearPrimary(stored.ProductId);
            }

            stored.SortOrder = assignment.SortOrder;
            stored.IsPrimary = assignment.IsPrimary;
            store.Images.Single(i => i.ImageId == stored.ImageId).Description = assignment.Image?.Description;
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(int productId, int imageId, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            if (Find(productId, imageId) is null)
            {
                return Task.FromResult(false);
            }

            store.RemoveImage(imageId);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<bool> ContainsAsync(int productId, int imageId, bool visibleOnly, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            return Task.FromResult(Find(productId, imageId) is not null && (!visibleOnly || IsVisible(productId)));
        }
    }

    // Called inside the lock.
    private ProductImage? Find(int productId, int imageId) =>
        store.ProductImages.Find(p => p.ImageId == imageId && p.ProductId == productId);

    // Called inside the lock. Active and in a visible category.
    private bool IsVisible(int productId) =>
        store.Products.Find(p => p.ProductId == productId) is { IsActive: true } product
        && store.Categories.Exists(c => c.CategoryId == product.CategoryId && c.IsVisible);

    // Called inside the lock.
    private void ClearPrimary(int productId)
    {
        foreach (var other in store.ProductImages.Where(p => p.ProductId == productId))
        {
            other.IsPrimary = false;
        }
    }
}
