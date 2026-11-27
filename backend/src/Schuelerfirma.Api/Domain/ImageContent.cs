namespace Schuelerfirma.Api.Domain;

/// <summary>
/// The stored picture of an <see cref="Image"/> (one to one, same key). A separate entity and table on purpose:
/// products are loaded with their images in every list, and the image data must never come along.
/// </summary>
public class ImageContent
{
    /// <summary>
    /// Largest image that fits into the column (<c>MEDIUMBLOB</c>, 16 MiB - 1 byte). The upload limit (#13) must stay below it.
    /// </summary>
    public const int DataMaxLength = 16_777_215;

    /// <summary>Primary key and at the same time the foreign key to the <see cref="Image"/>.</summary>
    public int ImageId { get; set; }

    /// <summary>The image file as delivered to visitors (already checked and without metadata).</summary>
    public byte[] Data { get; set; } = [];
}
