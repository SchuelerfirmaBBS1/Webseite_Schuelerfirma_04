using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Abstractions;

/// <summary>
/// Storage for products. Returned products are detached copies with <see cref="Product.Category"/> and
/// <see cref="Product.Images"/> (ordered by <see cref="ProductImage.SortOrder"/>) loaded.
/// </summary>
/// <remarks>
/// <para>
/// The rules for visitors live in the queries <see cref="GetVisibleProductsAsync"/> and
/// <see cref="GetVisibleProductByIdAsync"/>, not in the callers: only active products in visible categories. The EF
/// Core implementation therefore filters in SQL, and the contract tests hold every implementation to these rules.
/// Hiding the price is not a storage rule; the public response leaves it out.
/// </para>
/// <para>
/// EF Core: foreign key to <see cref="Category"/> without cascading delete (a missing category is reported as
/// <see cref="InvalidOperationException"/>), images and their image data (<see cref="ProductImageContent"/>) are deleted together with their product.
/// </para>
/// </remarks>
public interface IProductRepository
{
    /// <summary>Returns all products, optionally of one category, ordered by name (case-insensitive).</summary>
    Task<IReadOnlyList<Product>> GetAllAsync(int? categoryId = null, CancellationToken cancellationToken = default);

    /// <summary>Returns the product with the given id, or <c>null</c>.</summary>
    Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the products visitors may see – active and in a visible category –, optionally of one category,
    /// ordered by name (case-insensitive).
    /// </summary>
    Task<IReadOnlyList<Product>> GetVisibleProductsAsync(int? categoryId = null, CancellationToken cancellationToken = default);

    /// <summary>Returns the product if visitors may see it (active and in a visible category), otherwise <c>null</c>.</summary>
    Task<Product?> GetVisibleProductByIdAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a new product (without images) and sets <see cref="Product.ProductId"/> on <paramref name="product"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The category does not exist.</exception>
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves <see cref="Product.CategoryId"/>, <see cref="Product.Name"/>, <see cref="Product.Description"/>,
    /// <see cref="Product.Price"/>, <see cref="Product.IsPriceVisible"/> and <see cref="Product.IsActive"/>.
    /// Images are left alone.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The product does not exist.</exception>
    /// <exception cref="InvalidOperationException">The category does not exist.</exception>
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>Changes only <see cref="Product.IsPriceVisible"/>.</summary>
    /// <returns><c>false</c> if the product does not exist.</returns>
    Task<bool> SetPriceVisibilityAsync(int productId, bool isPriceVisible, CancellationToken cancellationToken = default);

    /// <summary>Deletes the product together with its image records (not the image files).</summary>
    /// <returns><c>false</c> if the product does not exist.</returns>
    Task<bool> DeleteAsync(int productId, CancellationToken cancellationToken = default);
}
