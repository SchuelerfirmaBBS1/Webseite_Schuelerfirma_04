using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Images;

/// <summary>
/// Whom an <see cref="Image"/> belongs to. Images are always addressed together with their owner, so an image id
/// with another owner is treated as unknown and a URL cannot be rewritten to reach an image of a hidden product.
/// </summary>
/// <remarks>The events (#14) add an <c>EventId</c> here and in <see cref="Image"/>.</remarks>
/// <param name="ProductId">The owning product.</param>
public readonly record struct ImageOwner(int? ProductId)
{
    /// <summary>The images of a product.</summary>
    public static ImageOwner Product(int productId) => new(productId);

    /// <summary>The owner of <paramref name="image"/>.</summary>
    public static ImageOwner Of(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        return new ImageOwner(image.ProductId);
    }

    /// <summary>Whether exactly one owner id is set.</summary>
    public bool IsValid => ProductId is not null;

    /// <summary>Sets the owner ids of <paramref name="image"/>.</summary>
    public void ApplyTo(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        image.ProductId = ProductId;
    }
}
