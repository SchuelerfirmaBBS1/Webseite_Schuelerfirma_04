namespace Schuelerfirma.Api.Domain;

/// <summary>
/// Assigns an <see cref="Image"/> to a <see cref="Product"/>, with its position and whether it is the main image.
/// An image belongs to at most one product (<see cref="ImageId"/> is the key).
/// </summary>
public class ProductImage
{
    /// <summary>The image; primary key, so an image cannot be assigned to two products.</summary>
    public int ImageId { get; set; }

    /// <summary>The image record (without data); loaded by the repositories.</summary>
    public Image? Image { get; set; }

    /// <summary>The product.</summary>
    public int ProductId { get; set; }

    /// <summary>The product.</summary>
    public Product? Product { get; set; }

    /// <summary>Position in the list of images of the product; lower values come first.</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether this is the main image of the product (at most one per product).</summary>
    public bool IsPrimary { get; set; }
}
