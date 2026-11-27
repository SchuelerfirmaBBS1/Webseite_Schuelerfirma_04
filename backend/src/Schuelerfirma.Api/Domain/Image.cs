namespace Schuelerfirma.Api.Domain;

/// <summary>
/// An image of the website: the data shown in lists. It knows nothing about who uses it; owners point to their
/// images through their own assignment (<see cref="ProductImage"/>, with the events #14 an event image). The picture
/// itself is stored as <see cref="ImageContent"/> in its own table, so loading a product never loads image data.
/// </summary>
/// <remarks>
/// A new kind of owner adds its own assignment and leaves this class as it is. Every image has exactly one
/// assignment: uploads create image and assignment together, and deleting the owner deletes its images (done by the
/// owner's repository, because the database only removes the assignment on its own).
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

    /// <summary>Original file name of the upload; only for display, never used as a path.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Optional description (alternative text); plain text, never HTML.</summary>
    public string? Description { get; set; }

    /// <summary>Media type of the file, e.g. <c>image/jpeg</c>.</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Size of <see cref="ImageContent.Data"/> in bytes.</summary>
    public long Length { get; set; }
}
