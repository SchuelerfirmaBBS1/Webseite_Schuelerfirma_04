using Microsoft.EntityFrameworkCore;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Images;

namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core / MySQL implementation of <see cref="IImageRepository"/>: reads one image file at a time, never together
/// with a list.
/// </summary>
public sealed class EfImageRepository(IDbContextFactory<SchuelerfirmaDbContext> contextFactory) : IImageRepository
{
    /// <inheritdoc />
    public async Task<ImageFile?> GetFileAsync(int imageId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Images
            .Where(image => image.ImageId == imageId)
            .Join(
                db.ImageContents,
                image => image.ImageId,
                content => content.ImageId,
                (image, content) => new ImageFile(image.ContentType, content.Data))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
