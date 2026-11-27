using Microsoft.EntityFrameworkCore;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core / MySQL implementation of <see cref="IImageRepository"/>. Record and image data are written in one
/// transaction; the image data is only ever read for a single image, never together with its owner.
/// </summary>
public sealed class EfImageRepository(IDbContextFactory<SchuelerfirmaDbContext> contextFactory) : IImageRepository
{
    /// <inheritdoc />
    public async Task AddAsync(Image image, byte[] data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(data);

        var owner = ImageOwner.Of(image);
        if (!owner.IsValid)
        {
            throw new ArgumentException("An image needs exactly one owner.", nameof(image));
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Copy without the navigation so EF does not try to insert the owner.
        var entity = new Image
        {
            FileName = image.FileName,
            Description = image.Description,
            ContentType = image.ContentType,
            Length = image.Length,
            SortOrder = image.SortOrder,
            IsPrimary = image.IsPrimary,
        };
        owner.ApplyTo(entity);

        try
        {
            if (entity.IsPrimary)
            {
                await ClearPrimaryAsync(db, owner, cancellationToken);
            }

            db.Images.Add(entity);
            await db.SaveChangesAsync(cancellationToken);

            // The content has no navigation to its image (see ImageContentConfiguration), so it needs the generated
            // id; the transaction keeps both rows together.
            db.ImageContents.Add(new ImageContent { ImageId = entity.ImageId, Data = data });
            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw MySqlExceptionTranslator.TranslateImageException(ex);
        }

        image.ImageId = entity.ImageId;
    }

    /// <inheritdoc />
    public async Task<Image?> GetAsync(ImageOwner owner, int imageId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await Owned(db, owner, imageId).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(Image image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);

        var owner = ImageOwner.Of(image);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (image.IsPrimary)
        {
            await ClearPrimaryAsync(db, owner, cancellationToken);
        }

        var updated = await Owned(db, owner, image.ImageId).ExecuteUpdateAsync(
            setters => setters
                .SetProperty(i => i.Description, image.Description)
                .SetProperty(i => i.SortOrder, image.SortOrder)
                .SetProperty(i => i.IsPrimary, image.IsPrimary),
            cancellationToken);

        if (updated == 0)
        {
            // Unknown image: also undo clearing the main image of the owner.
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(ImageOwner owner, int imageId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        // The foreign key of ImageContents deletes the image data (ON DELETE CASCADE).
        var deleted = await Owned(db, owner, imageId).ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    /// <inheritdoc />
    public async Task<ImageFile?> GetFileAsync(ImageOwner owner, int imageId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await FileQuery(db, Owned(db, owner, imageId)).SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ImageFile?> GetVisibleFileAsync(ImageOwner owner, int imageId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        // A product image: the product is active and in a visible category.
        var visible = Owned(db, owner, imageId).Where(i =>
            i.ProductId != null && i.Product!.IsActive && i.Product.Category!.IsVisible);
        return await FileQuery(db, visible).SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>The image with this id if it belongs to <paramref name="owner"/>; nothing for an invalid owner.</summary>
    private static IQueryable<Image> Owned(SchuelerfirmaDbContext db, ImageOwner owner, int imageId)
    {
        if (!owner.IsValid)
        {
            return db.Images.Where(_ => false);
        }

        var productId = owner.ProductId;
        return db.Images.Where(i => i.ImageId == imageId && i.ProductId == productId);
    }

    private static IQueryable<ImageFile> FileQuery(SchuelerfirmaDbContext db, IQueryable<Image> images) =>
        images.Join(
            db.ImageContents,
            image => image.ImageId,
            content => content.ImageId,
            (image, content) => new ImageFile(image.ContentType, content.Data));

    private static Task<int> ClearPrimaryAsync(SchuelerfirmaDbContext db, ImageOwner owner, CancellationToken cancellationToken)
    {
        var productId = owner.ProductId;
        return db.Images
            .Where(i => i.ProductId == productId && i.IsPrimary)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.IsPrimary, false), cancellationToken);
    }
}
