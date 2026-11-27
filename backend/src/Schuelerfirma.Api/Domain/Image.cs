namespace Schuelerfirma.Api.Domain;

/// <summary>
/// An image of the website: the data shown in lists. Every image belongs to exactly one owner – today a product
/// (<see cref="ProductId"/>), with the events (#14) also an event. The picture itself is stored as
/// <see cref="ImageContent"/> in its own table, so loading a product never loads image data.
/// </summary>
/// <remarks>
/// One model for all images instead of one per owner: storage, checks and delivery are the same for every image.
/// Exactly one owner id is set (check constraint <c>CK_Images_ExactlyOneOwner</c>, and the repositories check it
/// too). Deleting the owner deletes its images and their data.
/// </remarks>
public class Image
{
    /// <summary>Maximum length of <see cref="FileName"/>.</summary>
    public const int FileNameMaxLength = 255;

    /// <summary>Maximum length of <see cref="Description"/>.</summary>
    public const int DescriptionMaxLength = 500;

    /// <summary>Maximum length of <see cref="ContentType"/>.</summary>
    public const int ContentTypeMaxLength = 100;

    /// <summary>Primary key.</summary>
    public int ImageId { get; set; }

    /// <summary>The product the image belongs to, or <c>null</c> if it belongs to another owner.</summary>
    public int? ProductId { get; set; }

    /// <summary>The product, if the image belongs to one.</summary>
    public Product? Product { get; set; }

    /// <summary>Original file name of the upload; only for display, never used as a path.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Optional description (alternative text); plain text, never HTML.</summary>
    public string? Description { get; set; }

    /// <summary>Media type of the file, e.g. <c>image/jpeg</c>.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Size of <see cref="ImageContent.Data"/> in bytes.</summary>
    public long Length { get; set; }

    /// <summary>Position in the list of images of the owner; lower values come first.</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether this is the main image of its owner (at most one per owner).</summary>
    public bool IsPrimary { get; set; }
}
