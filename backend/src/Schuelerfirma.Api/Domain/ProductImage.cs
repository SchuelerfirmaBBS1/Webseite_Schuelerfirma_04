namespace Schuelerfirma.Api.Domain;

/// <summary>
/// An image of a product: the data shown in lists. The image itself is stored in the database as
/// <see cref="ProductImageContent"/>, in its own table, so loading a product never loads image data. The endpoints
/// for uploading and delivering images follow with #13.
/// </summary>
public class ProductImage
{
    /// <summary>Maximum length of <see cref="FileName"/>.</summary>
    public const int FileNameMaxLength = 255;

    /// <summary>Maximum length of <see cref="Description"/>.</summary>
    public const int DescriptionMaxLength = 500;

    /// <summary>Maximum length of <see cref="ContentType"/>.</summary>
    public const int ContentTypeMaxLength = 100;

    /// <summary>Primary key.</summary>
    public int ProductImageId { get; set; }

    /// <summary>The product the image belongs to; deleting the product deletes the image and its content.</summary>
    public int ProductId { get; set; }

    /// <summary>The product.</summary>
    public Product? Product { get; set; }

    /// <summary>Original file name of the upload; only for display, never used as a path.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Optional description (alternative text); plain text, never HTML.</summary>
    public string? Description { get; set; }

    /// <summary>Media type of the file, e.g. <c>image/jpeg</c>.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Size of <see cref="ProductImageContent.Data"/> in bytes.</summary>
    public long Length { get; set; }

    /// <summary>Position in the list of images; lower values come first.</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether this is the main image of the product (at most one per product).</summary>
    public bool IsPrimary { get; set; }
}
