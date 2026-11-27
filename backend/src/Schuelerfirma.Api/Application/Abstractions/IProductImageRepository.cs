using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Abstractions;

/// <summary>
/// Storage for product images: the records (<see cref="ProductImage"/>) and their image data
/// (<see cref="ProductImageContent"/>). Products load their image records through <see cref="IProductRepository"/>;
/// the image data is only read here, one image at a time.
/// </summary>
/// <remarks>
/// <para>
/// An image is always addressed together with its product: an image id with the id of another product is treated
/// as unknown, so a URL cannot be rewritten to reach an image of a hidden product.
/// </para>
/// <para>
/// At most one image per product is the main image: whenever an image is saved with
/// <see cref="ProductImage.IsPrimary"/> set, the other images of the product lose the flag in the same transaction.
/// </para>
/// </remarks>
public interface IProductImageRepository
{
    /// <summary>
    /// Saves a new image with its data in one transaction and sets <see cref="ProductImage.ProductImageId"/> on
    /// <paramref name="image"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The product does not exist.</exception>
    Task AddAsync(ProductImage image, byte[] data, CancellationToken cancellationToken = default);

    /// <summary>Returns the image record (without data), or <c>null</c> if the product has no such image.</summary>
    Task<ProductImage?> GetAsync(int productId, int productImageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves <see cref="ProductImage.Description"/>, <see cref="ProductImage.SortOrder"/> and
    /// <see cref="ProductImage.IsPrimary"/> of the image identified by <see cref="ProductImage.ProductId"/> and
    /// <see cref="ProductImage.ProductImageId"/>.
    /// </summary>
    /// <returns><c>false</c> if the product has no such image.</returns>
    Task<bool> UpdateAsync(ProductImage image, CancellationToken cancellationToken = default);

    /// <summary>Deletes the image together with its data.</summary>
    /// <returns><c>false</c> if the product has no such image.</returns>
    Task<bool> DeleteAsync(int productId, int productImageId, CancellationToken cancellationToken = default);

    /// <summary>Returns the image file for the admin area, whether visitors may see the product or not.</summary>
    Task<ImageFile?> GetFileAsync(int productId, int productImageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the image file only if visitors may see the product (active and in a visible category), otherwise
    /// <c>null</c> – the same rule as <see cref="IProductRepository.GetVisibleProductByIdAsync"/>, applied in the query.
    /// </summary>
    Task<ImageFile?> GetVisibleFileAsync(int productId, int productImageId, CancellationToken cancellationToken = default);
}
