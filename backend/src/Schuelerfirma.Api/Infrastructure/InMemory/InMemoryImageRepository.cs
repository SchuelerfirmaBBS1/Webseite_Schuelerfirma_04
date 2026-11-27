using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.InMemory;

/// <summary>In-memory <see cref="IImageRepository"/> for development and tests.</summary>
public sealed class InMemoryImageRepository(InMemoryStore store) : IImageRepository
{
    /// <inheritdoc />
    public Task AddAsync(Image image, byte[] data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(data);

        var owner = ImageOwner.Of(image);
        if (!owner.IsValid)
        {
            throw new ArgumentException("An image needs exactly one owner.", nameof(image));
        }

        lock (store.Sync)
        {
            if (!OwnerExists(owner))
            {
                throw new InvalidOperationException("Referenced owner does not exist.");
            }

            var stored = InMemoryStore.Copy(image);
            stored.ImageId = ++store.LastImageId;
            if (stored.IsPrimary)
            {
                ClearPrimary(owner);
            }

            store.Images.Add(stored);
            store.ImageContents[stored.ImageId] = new ImageContent { ImageId = stored.ImageId, Data = [.. data] };
            image.ImageId = stored.ImageId;
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task<Image?> GetAsync(ImageOwner owner, int imageId, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var stored = Find(owner, imageId);
            return Task.FromResult(stored is null ? null : InMemoryStore.Copy(stored));
        }
    }

    /// <inheritdoc />
    public Task<bool> UpdateAsync(Image image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);

        lock (store.Sync)
        {
            var owner = ImageOwner.Of(image);
            var stored = Find(owner, image.ImageId);
            if (stored is null)
            {
                return Task.FromResult(false);
            }

            if (image.IsPrimary)
            {
                ClearPrimary(owner);
            }

            stored.Description = image.Description;
            stored.SortOrder = image.SortOrder;
            stored.IsPrimary = image.IsPrimary;
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(ImageOwner owner, int imageId, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var stored = Find(owner, imageId);
            if (stored is null)
            {
                return Task.FromResult(false);
            }

            store.Images.Remove(stored);
            store.ImageContents.Remove(imageId);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc />
    public Task<ImageFile?> GetFileAsync(ImageOwner owner, int imageId, CancellationToken cancellationToken = default) =>
        File(owner, imageId, onlyVisible: false);

    /// <inheritdoc />
    public Task<ImageFile?> GetVisibleFileAsync(ImageOwner owner, int imageId, CancellationToken cancellationToken = default) =>
        File(owner, imageId, onlyVisible: true);

    private Task<ImageFile?> File(ImageOwner owner, int imageId, bool onlyVisible)
    {
        lock (store.Sync)
        {
            var stored = Find(owner, imageId);
            if (stored is null || !store.ImageContents.TryGetValue(imageId, out var content)
                || (onlyVisible && !IsVisible(owner)))
            {
                return Task.FromResult<ImageFile?>(null);
            }

            return Task.FromResult<ImageFile?>(new ImageFile(stored.ContentType, [.. content.Data]));
        }
    }

    // Called inside the lock. An invalid owner (no id set) matches nothing.
    private Image? Find(ImageOwner owner, int imageId) =>
        owner.IsValid ? store.Images.Find(i => i.ImageId == imageId && ImageOwner.Of(i) == owner) : null;

    // Called inside the lock.
    private bool OwnerExists(ImageOwner owner) => store.Products.Exists(p => p.ProductId == owner.ProductId);

    // Called inside the lock. A product: active and in a visible category.
    private bool IsVisible(ImageOwner owner) =>
        store.Products.Find(p => p.ProductId == owner.ProductId) is { IsActive: true } product
        && store.Categories.Exists(c => c.CategoryId == product.CategoryId && c.IsVisible);

    // Called inside the lock.
    private void ClearPrimary(ImageOwner owner)
    {
        foreach (var other in store.Images.Where(i => ImageOwner.Of(i) == owner))
        {
            other.IsPrimary = false;
        }
    }
}
