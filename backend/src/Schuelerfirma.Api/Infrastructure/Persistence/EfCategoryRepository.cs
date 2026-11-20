using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core / MySQL implementation of <see cref="ICategoryRepository"/>.
/// </summary>
public sealed class EfCategoryRepository(IDbContextFactory<SchuelerfirmaDbContext> contextFactory) : ICategoryRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ThenBy(c => c.CategoryId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Category>> GetVisibleAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Categories
            .AsNoTracking()
            .Where(c => c.IsVisible)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.CategoryId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Category?> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.CategoryId == categoryId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entity = new Category { Name = category.Name, IsVisible = category.IsVisible };
        db.Categories.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            category.CategoryId = entity.CategoryId;
        }
        catch (DbUpdateException ex)
        {
            throw MySqlExceptionTranslator.TranslateCategoryException(ex);
        }
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        int updated;
        try
        {
            updated = await db.Categories
                .Where(c => c.CategoryId == category.CategoryId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(c => c.Name, category.Name)
                        .SetProperty(c => c.IsVisible, category.IsVisible),
                    cancellationToken);
        }
        catch (Exception ex) when (ex is MySqlException or DbUpdateException)
        {
            throw MySqlExceptionTranslator.TranslateCategoryException(ex);
        }

        if (updated == 0)
        {
            throw new KeyNotFoundException($"Category with ID {category.CategoryId} was not found.");
        }
    }

    /// <inheritdoc />
    public async Task<CategoryDeleteResult> DeleteAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        if (await db.Products.AnyAsync(p => p.CategoryId == categoryId, cancellationToken))
        {
            return CategoryDeleteResult.HasProducts;
        }

        try
        {
            var deleted = await db.Categories
                .Where(c => c.CategoryId == categoryId)
                .ExecuteDeleteAsync(cancellationToken);
            return deleted == 0 ? CategoryDeleteResult.NotFound : CategoryDeleteResult.Deleted;
        }
        catch (Exception ex) when (ex is MySqlException or DbUpdateException && MySqlExceptionTranslator.IsRowStillReferenced(ex))
        {
            // A product was added between the check and the delete; the foreign key kept the category.
            return CategoryDeleteResult.HasProducts;
        }
    }
}
