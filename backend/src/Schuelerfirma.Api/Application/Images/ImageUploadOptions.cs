using System.ComponentModel.DataAnnotations;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Images;

/// <summary>
/// Limits for uploaded images, bound from <c>Images</c>. Invalid values stop the application at startup.
/// </summary>
/// <remarks>
/// The reverse proxy has its own limit for request bodies (<c>MAX_REQUEST_BODY</c> in <c>deploy/.env</c>); it must be
/// at least <see cref="MaxRequestSizeBytes"/> plus a little room for the multipart framing.
/// </remarks>
public sealed class ImageUploadOptions : IValidatableObject
{
    /// <summary>Configuration section these options are bound from.</summary>
    public const string SectionName = "Images";

    /// <summary>
    /// Largest file per image in bytes, for the upload and for the processed image that is stored. At most
    /// <see cref="ImageContent.DataMaxLength"/> (column size). Larger uploads are answered with <c>413</c>.
    /// </summary>
    [Range(1, ImageContent.DataMaxLength, ErrorMessage = "Images:MaxFileSizeBytes must be between 1 and 16777215.")]
    public int MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Largest request body of an upload in bytes (all files together). Larger requests get <c>413</c>.</summary>
    [Range(1, 1024L * 1024 * 1024, ErrorMessage = "Images:MaxRequestSizeBytes must be between 1 and 1 GiB.")]
    public long MaxRequestSizeBytes { get; set; } = 30 * 1024 * 1024;

    /// <summary>Most files in one upload.</summary>
    [Range(1, 100, ErrorMessage = "Images:MaxFilesPerRequest must be between 1 and 100.")]
    public int MaxFilesPerRequest { get; set; } = 10;

    /// <summary>Longest edge of a stored image in pixels; larger images are scaled down (aspect ratio kept).</summary>
    [Range(100, 10_000, ErrorMessage = "Images:MaxDimension must be between 100 and 10000.")]
    public int MaxDimension { get; set; } = 2000;

    /// <summary>
    /// Most pixels (width × height) an uploaded image may have. Checked from the file header before decoding, so a
    /// small file that claims a huge image (decompression bomb) never gets its memory.
    /// </summary>
    [Range(1, 500_000_000, ErrorMessage = "Images:MaxSourcePixels must be between 1 and 500000000.")]
    public long MaxSourcePixels { get; set; } = 40_000_000;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxRequestSizeBytes < MaxFileSizeBytes)
        {
            yield return new ValidationResult(
                "Images:MaxRequestSizeBytes must be at least Images:MaxFileSizeBytes.",
                [nameof(MaxRequestSizeBytes)]);
        }
    }
}
