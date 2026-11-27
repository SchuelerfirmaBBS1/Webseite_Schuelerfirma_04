namespace Schuelerfirma.Api.Application.Images;

/// <summary>
/// Turns an uploaded file into the image that is stored and shown to visitors. Every uploaded image goes through
/// here; the original file is never stored.
/// </summary>
/// <remarks>
/// The format is detected from the content, not from the file name or the <c>Content-Type</c> of the upload. Only
/// JPEG, PNG and WebP are accepted; SVG never, because an image comes from the same domain as the admin area and a
/// script inside an SVG would run with the viewer's session. The result is newly encoded pixel data without any
/// metadata (EXIF with the GPS position, XMP, IPTC, color profiles), turned upright according to the EXIF
/// orientation and scaled down to <see cref="ImageUploadOptions.MaxDimension"/>.
/// </remarks>
public interface IImageProcessor
{
    /// <summary>Checks and converts <paramref name="file"/>.</summary>
    /// <param name="file">The uploaded file.</param>
    /// <returns>The processed image, or why the file was rejected.</returns>
    ImageProcessingResult Process(ReadOnlySpan<byte> file);
}

/// <summary>Why an uploaded file was rejected.</summary>
public enum ImageRejection
{
    /// <summary>The file was accepted.</summary>
    None,

    /// <summary>Not a JPEG, PNG or WebP image (judged by the content).</summary>
    UnsupportedFormat,

    /// <summary>The image has more pixels than <see cref="ImageUploadOptions.MaxSourcePixels"/>.</summary>
    TooManyPixels,

    /// <summary>The processed image is still larger than <see cref="ImageUploadOptions.MaxFileSizeBytes"/>.</summary>
    TooLarge,

    /// <summary>The file claims to be a supported image but cannot be decoded.</summary>
    Corrupt,
}

/// <summary>An image ready to be stored.</summary>
/// <param name="Data">The newly encoded file.</param>
/// <param name="ContentType">Media type of <paramref name="Data"/>: <c>image/jpeg</c>, <c>image/png</c> or <c>image/webp</c>.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
public sealed record ProcessedImage(byte[] Data, string ContentType, int Width, int Height);

/// <summary>Result of <see cref="IImageProcessor.Process"/>.</summary>
/// <param name="Image">The processed image if the file was accepted.</param>
/// <param name="Rejection">Why the file was rejected, or <see cref="ImageRejection.None"/>.</param>
public sealed record ImageProcessingResult(ProcessedImage? Image, ImageRejection Rejection)
{
    /// <summary>The file was accepted.</summary>
    public static ImageProcessingResult Accepted(ProcessedImage image) => new(image, ImageRejection.None);

    /// <summary>The file was rejected.</summary>
    public static ImageProcessingResult Rejected(ImageRejection rejection) => new(null, rejection);
}
