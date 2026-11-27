using Microsoft.EntityFrameworkCore;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core / MySQL implementation of <see cref="IProductImageRepository"/>. Image, image data and assignment are
/// written in one transaction.
/// </summary>
public sealed class EfProductImageRepository(IDbContextFactory<SchuelerfirmaDbContext> contextFactory) : IProductImageRepository
{
    /// <inheritdoc />
    public async Task AddAsync(ProductImage assignment, Image image, byte[] data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(data);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Copies without navigations, so EF does not try to insert the product.
        var imageEntity = new Image
        {
            FileName = image.FileName,
            Description = image.Description,
            ContentType = image.ContentType,
            Length = image.Length,
        };
        var assignmentEntity = new ProductImage
        {
            Image = imageEntity,
            ProductId = assignment.ProductId,
            SortOrder = assignment.SortOrder,
            IsPrimary = assignment.IsPrimary,
        };

        try
        {
            if (assignmentEntity.IsPrimary)
            {
                await ClearPrimaryAsync(db, assignmentEntity.ProductId, cancellationToken);
            }

            db.ProductImages.Add(assignmentEntity);
            await db.SaveChangesAsync(cancellationToken);

            // The content has no navigation to its image (see ImageContentConfiguration), so it needs the generated
            // id; the transaction keeps all rows together.
            db.ImageContents.Add(new ImageContent { ImageId = imageEntity.ImageId, Data = data });
            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw MySqlExceptionTranslator.TranslateProductImageException(ex);
        }

        image.ImageId = imageEntity.ImageId;
        assignment.ImageId = imageEntity.ImageId;
    }

    /// <inheritdoc />
    public async Task<ProductImage?> GetAsync(int productId, int imageId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await Assignment(db, productId, imageId)
            .AsNoTracking()
            .Include(p => p.Image)
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(ProductImage assignment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (assignment.IsPrimary)
        {
            await ClearPrimaryAsync(db, assignment.ProductId, cancellationToken);
        }

        var updated = await Assignment(db, assignment.ProductId, assignment.ImageId).ExecuteUpdateAsync(
            setters => setters
                .SetProperty(p => p.SortOrder, assignment.SortOrder)
                .SetProperty(p => p.IsPrimary, assignment.IsPrimary),
            cancellationToken);

        if (updated == 0)
        {
            // Unknown image: also undo clearing the main image of the product.
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var description = assignment.Image?.Description;
        await db.Images
            .Where(i => i.ImageId == assignment.ImageId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.Description, description), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int productId, int imageId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        // Deleting the image removes its data and its assignment (ON DELETE CASCADE).
        var deleted = await db.Images
            .Where(i => i.ImageId == imageId && db.ProductImages.Any(p => p.ImageId == i.ImageId && p.ProductId == productId))
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    /// <inheritdoc />
    public async Task<bool> ContainsAsync(int productId, int imageId, bool visibleOnly, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await Assignment(db, productId, imageId)
            .AnyAsync(p => !visibleOnly || (p.Product!.IsActive && p.Product.Category!.IsVisible), cancellationToken);
    }

    private static IQueryable<ProductImage> Assignment(SchuelerfirmaDbContext db, int productId, int imageId) =>
        db.ProductImages.Where(p => p.ImageId == imageId && p.ProductId == productId);

    private static Task<int> ClearPrimaryAsync(SchuelerfirmaDbContext db, int productId, CancellationToken cancellationToken) =>
        db.ProductImages
            .Where(p => p.ProductId == productId && p.IsPrimary)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.IsPrimary, false), cancellationToken);
}
