using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Abstractions;

/// <summary>
/// Reads the stored image files (<see cref="ImageContent"/>) of all owners. It knows nothing about owners: whether a
/// caller may see an image is decided before, by the owner's repository (e.g.
/// <see cref="IProductImageRepository.ContainsAsync"/>).
/// </summary>
/// <remarks>
/// Images are written by the owner's repository, together with the assignment in one transaction, so there is no
/// image without owner. The image data is only ever read here, one image at a time, never together with a list.
/// </remarks>
public interface IImageRepository
{
    /// <summary>Returns the stored file of the image, or <c>null</c> if there is no such image.</summary>
    Task<ImageFile?> GetFileAsync(int imageId, CancellationToken cancellationToken = default);
}
