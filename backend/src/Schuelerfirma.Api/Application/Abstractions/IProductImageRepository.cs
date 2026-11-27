using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Abstractions;

/// <summary>
/// The images of products: creates, changes and deletes images together with their assignment
/// (<see cref="ProductImage"/>). Products load their assignments with the image records through
/// <see cref="IProductRepository"/>; the files are read through <see cref="IImageRepository"/>.
/// </summary>
/// <remarks>
/// <para>
/// An image is always addressed together with its product: an image id with the id of another product is treated
/// as unknown, so a URL cannot be rewritten to reach an image of a hidden product.
/// </para>
/// <para>
/// At most one image per product is the main image: whenever an assignment is saved with
/// <see cref="ProductImage.IsPrimary"/> set, the other images of the product lose the flag in the same transaction.
/// Deleting a product deletes its images (<see cref="IProductRepository.DeleteAsync"/>).
/// </para>
/// </remarks>
public interface IProductImageRepository
{
    /// <summary>
    /// Saves a new image with its data and assigns it to <see cref="ProductImage.ProductId"/> of
    /// <paramref name="assignment"/> (position and main-image flag from there), all in one transaction. Sets
    /// <see cref="Image.ImageId"/> on <paramref name="image"/> and <see cref="ProductImage.ImageId"/> on
    /// <paramref name="assignment"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The product does not exist.</exception>
    Task AddAsync(ProductImage assignment, Image image, byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the assignment with <see cref="ProductImage.Image"/> loaded, or <c>null</c> if the product has no such
    /// image.
    /// </summary>
    Task<ProductImage?> GetAsync(int productId, int imageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves <see cref="ProductImage.SortOrder"/>, <see cref="ProductImage.IsPrimary"/> and the
    /// <see cref="Image.Description"/> of <see cref="ProductImage.Image"/> for the image identified by
    /// <see cref="ProductImage.ProductId"/> and <see cref="ProductImage.ImageId"/>.
    /// </summary>
    /// <returns><c>false</c> if the product has no such image.</returns>
    Task<bool> UpdateAsync(ProductImage assignment, CancellationToken cancellationToken = default);

    /// <summary>Deletes the image with its data and its assignment.</summary>
    /// <returns><c>false</c> if the product has no such image.</returns>
    Task<bool> DeleteAsync(int productId, int imageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the product has this image. With <paramref name="visibleOnly"/> only if visitors may see the product
    /// (active and in a visible category) – the same rule as <see cref="IProductRepository.GetVisibleProductByIdAsync"/>,
    /// applied in the query.
    /// </summary>
    Task<bool> ContainsAsync(int productId, int imageId, bool visibleOnly, CancellationToken cancellationToken = default);
}
