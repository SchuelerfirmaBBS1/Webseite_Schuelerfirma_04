using Microsoft.EntityFrameworkCore;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core / MySQL implementation of <see cref="IProductImageRepository"/>. Record and image data are written in
/// one transaction; the image data is only ever read for a single image, never together with a product.
/// </summary>
public sealed class EfProductImageRepository(IDbContextFactory<SchuelerfirmaDbContext> contextFactory) : IProductImageRepository
{
    /// <inheritdoc />
    public async Task AddAsync(ProductImage image, byte[] data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(data);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Copy without the navigation so EF does not try to insert the product.
        var entity = new ProductImage
        {
            ProductId = image.ProductId,
            FileName = image.FileName,
            Description = image.Description,
            ContentType = image.ContentType,
            Length = image.Length,
            SortOrder = image.SortOrder,
            IsPrimary = image.IsPrimary,
        };

        try
        {
            if (entity.IsPrimary)
            {
                await ClearPrimaryAsync(db, entity.ProductId, cancellationToken);
            }

            db.ProductImages.Add(entity);
            await db.SaveChangesAsync(cancellationToken);

            // The content has no navigation to its image (see ProductImageContentConfiguration), so it needs the
            // generated id; the transaction keeps both rows together.
            db.ProductImageContents.Add(new ProductImageContent { ProductImageId = entity.ProductImageId, Data = data });
            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw MySqlExceptionTranslator.TranslateProductImageException(ex);
        }

        image.ProductImageId = entity.ProductImageId;
    }

    /// <inheritdoc />
    public async Task<ProductImage?> GetAsync(int productId, int productImageId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.ProductImages
            .AsNoTracking()
            .SingleOrDefaultAsync(i => i.ProductImageId == productImageId && i.ProductId == productId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(ProductImage image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (image.IsPrimary)
        {
            await ClearPrimaryAsync(db, image.ProductId, cancellationToken);
        }

        var updated = await db.ProductImages
            .Where(i => i.ProductImageId == image.ProductImageId && i.ProductId == image.ProductId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(i => i.Description, image.Description)
                    .SetProperty(i => i.SortOrder, image.SortOrder)
                    .SetProperty(i => i.IsPrimary, image.IsPrimary),
                cancellationToken);

        if (updated == 0)
        {
            // Unknown image: also undo clearing the main image of the product.
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int productId, int productImageId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        // The foreign key of ProductImageContents deletes the image data (ON DELETE CASCADE).
        var deleted = await db.ProductImages
            .Where(i => i.ProductImageId == productImageId && i.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    /// <inheritdoc />
    public async Task<ImageFile?> GetFileAsync(int productId, int productImageId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await FileQuery(db, db.ProductImages.Where(i => i.ProductImageId == productImageId && i.ProductId == productId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ImageFile?> GetVisibleFileAsync(int productId, int productImageId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var visibleImages = db.ProductImages.Where(i =>
            i.ProductImageId == productImageId
            && i.ProductId == productId
            && i.Product!.IsActive
            && i.Product.Category!.IsVisible);
        return await FileQuery(db, visibleImages).SingleOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<ImageFile> FileQuery(SchuelerfirmaDbContext db, IQueryable<ProductImage> images) =>
        images.Join(
            db.ProductImageContents,
            image => image.ProductImageId,
            content => content.ProductImageId,
            (image, content) => new ImageFile(image.ContentType, content.Data));

    private static Task<int> ClearPrimaryAsync(SchuelerfirmaDbContext db, int productId, CancellationToken cancellationToken) =>
        db.ProductImages
            .Where(i => i.ProductId == productId && i.IsPrimary)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.IsPrimary, false), cancellationToken);
}
