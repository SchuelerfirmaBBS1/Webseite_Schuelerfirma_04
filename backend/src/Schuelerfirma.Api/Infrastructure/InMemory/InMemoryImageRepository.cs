using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Images;

namespace Schuelerfirma.Api.Infrastructure.InMemory;

/// <summary>In-memory <see cref="IImageRepository"/> for development and tests.</summary>
public sealed class InMemoryImageRepository(InMemoryStore store) : IImageRepository
{
    /// <inheritdoc />
    public Task<ImageFile?> GetFileAsync(int imageId, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var image = store.Images.Find(i => i.ImageId == imageId);
            if (image is null || !store.ImageContents.TryGetValue(imageId, out var content))
            {
                return Task.FromResult<ImageFile?>(null);
            }

            return Task.FromResult<ImageFile?>(new ImageFile(image.ContentType, [.. content.Data]));
        }
    }
}
