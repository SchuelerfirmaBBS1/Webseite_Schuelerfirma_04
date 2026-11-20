namespace Schuelerfirma.Api.Domain;

/// <summary>
/// An image of a product. The file itself lives in the image storage (#13); the endpoints for uploading and
/// delivering it follow together with that storage.
/// </summary>
public class ProductImage
{
    /// <summary>Maximum length of <see cref="FileName"/>.</summary>
    public const int FileNameMaxLength = 255;

    /// <summary>Maximum length of <see cref="Description"/>.</summary>
    public const int DescriptionMaxLength = 500;

    /// <summary>Maximum length of <see cref="ContentType"/>.</summary>
    public const int ContentTypeMaxLength = 100;

    /// <summary>Maximum length of <see cref="FilePath"/>.</summary>
    public const int FilePathMaxLength = 255;

    /// <summary>Primary key.</summary>
    public int ProductImageId { get; set; }

    /// <summary>The product the image belongs to; deleting the product deletes the image.</summary>
    public int ProductId { get; set; }

    /// <summary>The product.</summary>
    public Product? Product { get; set; }

    /// <summary>Original file name of the upload; only for display, never used as a path.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Optional description (alternative text); plain text, never HTML.</summary>
    public string? Description { get; set; }

    /// <summary>Media type of the file, e.g. <c>image/jpeg</c>.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Location of the file inside the image storage, chosen by the server.</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Position in the list of images; lower values come first.</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether this is the main image of the product (at most one per product).</summary>
    public bool IsPrimary { get; set; }
}
