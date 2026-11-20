using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.InMemory;

/// <summary>In-memory <see cref="ICategoryRepository"/> for development and tests.</summary>
public sealed class InMemoryCategoryRepository(InMemoryStore store) : ICategoryRepository
{
    /// <inheritdoc />
    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Query(_ => true);

    /// <inheritdoc />
    public Task<IReadOnlyList<Category>> GetVisibleAsync(CancellationToken cancellationToken = default) =>
        Query(c => c.IsVisible);

    /// <inheritdoc />
    public Task<Category?> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var category = store.Categories.Find(c => c.CategoryId == categoryId);
            return Task.FromResult(category is null ? null : InMemoryStore.Copy(category));
        }
    }

    /// <inheritdoc />
    public Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            EnsureNameIsFree(category.Name, exceptId: null);

            var stored = InMemoryStore.Copy(category);
            stored.CategoryId = ++store.LastCategoryId;
            store.Categories.Add(stored);
            category.CategoryId = stored.CategoryId;
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            var stored = store.Categories.Find(c => c.CategoryId == category.CategoryId)
                ?? throw new KeyNotFoundException($"Category with ID {category.CategoryId} was not found.");
            EnsureNameIsFree(category.Name, exceptId: category.CategoryId);

            stored.Name = category.Name;
            stored.IsVisible = category.IsVisible;
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task<CategoryDeleteResult> DeleteAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        lock (store.Sync)
        {
            if (!store.Categories.Exists(c => c.CategoryId == categoryId))
            {
                return Task.FromResult(CategoryDeleteResult.NotFound);
            }

            if (store.Products.Exists(p => p.CategoryId == categoryId))
            {
                return Task.FromResult(CategoryDeleteResult.HasProducts);
            }

            store.Categories.RemoveAll(c => c.CategoryId == categoryId);
            return Task.FromResult(CategoryDeleteResult.Deleted);
        }
    }

    private Task<IReadOnlyList<Category>> Query(Func<Category, bool> filter)
    {
        lock (store.Sync)
        {
            IReadOnlyList<Category> categories =
            [
                .. store.Categories
                    .Where(filter)
                    .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(c => c.CategoryId)
                    .Select(InMemoryStore.Copy),
            ];
            return Task.FromResult(categories);
        }
    }

    private void EnsureNameIsFree(string name, int? exceptId)
    {
        if (store.Categories.Exists(c => c.CategoryId != exceptId && InMemoryStore.SameText(c.Name, name)))
        {
            throw new DuplicateEntryException(nameof(Category.Name));
        }
    }
}
