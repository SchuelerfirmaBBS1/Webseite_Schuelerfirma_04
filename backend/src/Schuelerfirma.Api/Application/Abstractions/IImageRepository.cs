using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Abstractions;

/// <summary>
/// Storage for all images of the website: the records (<see cref="Image"/>) and their data
/// (<see cref="ImageContent"/>). Owners load their image records themselves (e.g. <see cref="IProductRepository"/>);
/// the image data is only read here, one image at a time.
/// </summary>
/// <remarks>
/// <para>
/// An image is always addressed together with its <see cref="ImageOwner"/>: an image id with another owner is
/// treated as unknown, so a URL cannot be rewritten to reach an image of a hidden product.
/// </para>
/// <para>
/// At most one image per owner is the main image: whenever an image is saved with <see cref="Image.IsPrimary"/> set,
/// the other images of the same owner lose the flag in the same transaction.
/// </para>
/// </remarks>
public interface IImageRepository
{
    /// <summary>
    /// Saves a new image with its data in one transaction and sets <see cref="Image.ImageId"/> on
    /// <paramref name="image"/>. The owner is taken from the image (e.g. <see cref="Image.ProductId"/>).
    /// </summary>
    /// <exception cref="ArgumentException">Not exactly one owner is set.</exception>
    /// <exception cref="InvalidOperationException">The owner does not exist.</exception>
    Task AddAsync(Image image, byte[] data, CancellationToken cancellationToken = default);

    /// <summary>Returns the image record (without data), or <c>null</c> if the owner has no such image.</summary>
    Task<Image?> GetAsync(ImageOwner owner, int imageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves <see cref="Image.Description"/>, <see cref="Image.SortOrder"/> and <see cref="Image.IsPrimary"/> of the
    /// image identified by <see cref="Image.ImageId"/> and its owner.
    /// </summary>
    /// <returns><c>false</c> if the owner has no such image.</returns>
    Task<bool> UpdateAsync(Image image, CancellationToken cancellationToken = default);

    /// <summary>Deletes the image together with its data.</summary>
    /// <returns><c>false</c> if the owner has no such image.</returns>
    Task<bool> DeleteAsync(ImageOwner owner, int imageId, CancellationToken cancellationToken = default);

    /// <summary>Returns the image file for the admin area, whether visitors may see the owner or not.</summary>
    Task<ImageFile?> GetFileAsync(ImageOwner owner, int imageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the image file only if visitors may see the owner, otherwise <c>null</c>. For a product: active and in
    /// a visible category – the same rule as <see cref="IProductRepository.GetVisibleProductByIdAsync"/>, applied in
    /// the query.
    /// </summary>
    Task<ImageFile?> GetVisibleFileAsync(ImageOwner owner, int imageId, CancellationToken cancellationToken = default);
}
