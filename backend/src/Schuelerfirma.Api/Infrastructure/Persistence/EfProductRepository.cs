using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core / MySQL implementation of <see cref="IProductRepository"/>. The rules for visitors are part of the SQL
/// query, so hidden products never leave the database for a public request.
/// </summary>
public sealed class EfProductRepository(IDbContextFactory<SchuelerfirmaDbContext> contextFactory) : IProductRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetAllAsync(int? categoryId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await Ordered(Loaded(db).Where(p => categoryId == null || p.CategoryId == categoryId))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await Loaded(db).SingleOrDefaultAsync(p => p.ProductId == productId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Product>> GetVisibleProductsAsync(int? categoryId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await Ordered(Visible(Loaded(db)).Where(p => categoryId == null || p.CategoryId == categoryId))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Product?> GetVisibleProductByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await Visible(Loaded(db)).SingleOrDefaultAsync(p => p.ProductId == productId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        // Copy without navigations so EF does not try to insert the category or images.
        var entity = new Product
        {
            CategoryId = product.CategoryId,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            IsPriceVisible = product.IsPriceVisible,
            IsActive = product.IsActive,
        };
        db.Products.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            product.ProductId = entity.ProductId;
        }
        catch (DbUpdateException ex)
        {
            throw MySqlExceptionTranslator.TranslateProductException(ex);
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        int updated;
        try
        {
            updated = await db.Products
                .Where(p => p.ProductId == product.ProductId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(p => p.CategoryId, product.CategoryId)
                        .SetProperty(p => p.Name, product.Name)
                        .SetProperty(p => p.Description, product.Description)
                        .SetProperty(p => p.Price, product.Price)
                        .SetProperty(p => p.IsPriceVisible, product.IsPriceVisible)
                        .SetProperty(p => p.IsActive, product.IsActive),
                    cancellationToken);
        }
        catch (Exception ex) when (ex is MySqlException or DbUpdateException)
        {
            throw MySqlExceptionTranslator.TranslateProductException(ex);
        }

        if (updated == 0)
        {
            throw new KeyNotFoundException($"Product with ID {product.ProductId} was not found.");
        }
    }

    /// <inheritdoc />
    public async Task<bool> SetPriceVisibilityAsync(int productId, bool isPriceVisible, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var updated = await db.Products
            .Where(p => p.ProductId == productId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.IsPriceVisible, isPriceVisible), cancellationToken);
        return updated > 0;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int productId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        // The foreign key of ProductImages deletes the image rows (ON DELETE CASCADE).
        var deleted = await db.Products
            .Where(p => p.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    private static IQueryable<Product> Loaded(SchuelerfirmaDbContext db) =>
        db.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Category)
            .Include(p => p.Images.OrderBy(i => i.SortOrder).ThenBy(i => i.ProductImageId));

    private static IQueryable<Product> Visible(IQueryable<Product> products) =>
        products.Where(p => p.IsActive && p.Category!.IsVisible);

    private static IQueryable<Product> Ordered(IQueryable<Product> products) =>
        products.OrderBy(p => p.Name).ThenBy(p => p.ProductId);
}
