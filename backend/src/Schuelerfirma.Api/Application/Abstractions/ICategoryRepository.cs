using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Abstractions;

/// <summary>Outcome of <see cref="ICategoryRepository.DeleteAsync"/>.</summary>
public enum CategoryDeleteResult
{
    /// <summary>The category was deleted.</summary>
    Deleted,

    /// <summary>There is no such category.</summary>
    NotFound,

    /// <summary>The category still contains products and was kept.</summary>
    HasProducts,
}

/// <summary>
/// Storage for product categories. Returned categories are detached copies without their products.
/// </summary>
/// <remarks>
/// EF Core: unique index <c>UX_Categories_Name</c> with a case-insensitive collation; a violation is reported as
/// <see cref="DuplicateEntryException"/>. Products reference their category without cascading delete, so a category
/// with products cannot disappear (MySQL error 1451 → <see cref="CategoryDeleteResult.HasProducts"/>).
/// </remarks>
public interface ICategoryRepository
{
    /// <summary>Returns all categories ordered by name (case-insensitive).</summary>
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the categories visitors may see (<see cref="Category.IsVisible"/>), ordered by name.</summary>
    Task<IReadOnlyList<Category>> GetVisibleAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the category with the given id, or <c>null</c>.</summary>
    Task<Category?> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default);

    /// <summary>Saves a new category and sets <see cref="Category.CategoryId"/> on <paramref name="category"/>.</summary>
    /// <exception cref="DuplicateEntryException">The name is already taken (case-insensitive).</exception>
    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>Saves <see cref="Category.Name"/> and <see cref="Category.IsVisible"/>.</summary>
    /// <exception cref="KeyNotFoundException">The category does not exist.</exception>
    /// <exception cref="DuplicateEntryException">The name is already taken by another category.</exception>
    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>Deletes the category unless it still contains products.</summary>
    Task<CategoryDeleteResult> DeleteAsync(int categoryId, CancellationToken cancellationToken = default);
}
