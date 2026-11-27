using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Images;
using SkiaSharp;

namespace Schuelerfirma.Api.Infrastructure.Images;

/// <summary><see cref="IImageProcessor"/> on top of SkiaSharp (MIT license).</summary>
/// <remarks>
/// <para>
/// Steps: let the codec detect the format from the content and accept only JPEG, PNG and WebP; read only the header
/// to check the pixel count; decode the first frame into sRGB (embedded color profiles are converted); draw it
/// upright according to the EXIF orientation and scaled down into a fresh bitmap; encode that bitmap again in the
/// same format.
/// </para>
/// <para>
/// The new file is written from pixels only: the target bitmap has no color space, so the encoders write no color
/// profile either, and SkiaSharp's encoders never copy EXIF, XMP or text chunks. Nothing of the uploaded file
/// survives except the picture itself.
/// </para>
/// </remarks>
public sealed class SkiaImageProcessor(IOptions<ImageUploadOptions> options) : IImageProcessor
{
    /// <summary>Quality for JPEG and lossy WebP (1–100); good for photos at a fraction of the original size.</summary>
    public const int Quality = 85;

    /// <summary>zlib level for PNG (0–9).</summary>
    public const int PngCompressionLevel = 6;

    private readonly ImageUploadOptions settings = options.Value;

    /// <inheritdoc />
    public ImageProcessingResult Process(ReadOnlySpan<byte> file)
    {
        using var data = SKData.CreateCopy(file);
        using var codec = SKCodec.Create(data);

        // No codec: Skia does not recognize the content as any image (e.g. SVG, HTML, text).
        if (codec is null || !TryGetContentType(codec.EncodedFormat, out var contentType))
        {
            return ImageProcessingResult.Rejected(ImageRejection.UnsupportedFormat);
        }

        var source = codec.Info;
        if (source.Width <= 0 || source.Height <= 0)
        {
            return ImageProcessingResult.Rejected(ImageRejection.Corrupt);
        }

        if ((long)source.Width * source.Height > settings.MaxSourcePixels)
        {
            return ImageProcessingResult.Rejected(ImageRejection.TooManyPixels);
        }

        var alphaType = source.AlphaType == SKAlphaType.Opaque ? SKAlphaType.Opaque : SKAlphaType.Premul;
        var decodeInfo = new SKImageInfo(source.Width, source.Height, SKColorType.Rgba8888, alphaType, SKColorSpace.CreateSrgb());
        using var decoded = new SKBitmap(decodeInfo);

        // Only a complete decode counts; a truncated file (IncompleteInput) would be stored half grey.
        if (codec.GetPixels(decodeInfo, decoded.GetPixels()) != SKCodecResult.Success)
        {
            return ImageProcessingResult.Rejected(ImageRejection.Corrupt);
        }

        using var upright = Transform(decoded, codec.EncodedOrigin, alphaType);
        using var pixmap = upright.PeekPixels();
        using var encoded = Encode(pixmap, codec.EncodedFormat);
        if (encoded is null)
        {
            return ImageProcessingResult.Rejected(ImageRejection.Corrupt);
        }

        if (encoded.Size > settings.MaxFileSizeBytes)
        {
            return ImageProcessingResult.Rejected(ImageRejection.TooLarge);
        }

        return ImageProcessingResult.Accepted(new ProcessedImage(encoded.ToArray(), contentType, upright.Width, upright.Height));
    }

    private static bool TryGetContentType(SKEncodedImageFormat format, out string contentType)
    {
        contentType = format switch
        {
            SKEncodedImageFormat.Jpeg => "image/jpeg",
            SKEncodedImageFormat.Png => "image/png",
            SKEncodedImageFormat.Webp => "image/webp",
            _ => string.Empty,
        };
        return contentType.Length > 0;
    }

    /// <summary>Draws <paramref name="source"/> upright and scaled down into a new bitmap without color space.</summary>
    private SKBitmap Transform(SKBitmap source, SKEncodedOrigin origin, SKAlphaType alphaType)
    {
        float width = source.Width;
        float height = source.Height;

        // Orientations 5–8 turn the image by 90 degrees, so width and height swap.
        var swapsSides = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var uprightWidth = swapsSides ? height : width;
        var uprightHeight = swapsSides ? width : height;

        var scale = Math.Min(1f, settings.MaxDimension / Math.Max(uprightWidth, uprightHeight));
        var targetWidth = Math.Max(1, (int)Math.Round(uprightWidth * scale));
        var targetHeight = Math.Max(1, (int)Math.Round(uprightHeight * scale));

        var target = new SKBitmap(new SKImageInfo(targetWidth, targetHeight, SKColorType.Rgba8888, alphaType));
        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale(targetWidth / uprightWidth, targetHeight / uprightHeight);
        var orientation = OrientationMatrix(origin, width, height);
        canvas.Concat(in orientation);

        using var image = SKImage.FromBitmap(source);
        canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear), null);
        canvas.Flush();
        return target;
    }

    /// <summary>
    /// Maps the stored pixels to the upright picture for each EXIF orientation:
    /// <c>x' = ScaleX·x + SkewX·y + TransX</c>, <c>y' = SkewY·x + ScaleY·y + TransY</c>.
    /// </summary>
    private static SKMatrix OrientationMatrix(SKEncodedOrigin origin, float width, float height) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),         // 2: mirrored
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1), // 3: turned by 180°
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),       // 4: upside down
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),                // 5: transposed
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, height, 1, 0, 0, 0, 0, 1),         // 6: turned by 90° clockwise
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, height, -1, 0, width, 0, 0, 1), // 7: transversed
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, width, 0, 0, 1),        // 8: turned by 90° counterclockwise
        _ => SKMatrix.Identity,                                                            // 1: upright
    };

    private static SKData? Encode(SKPixmap pixmap, SKEncodedImageFormat format) => format switch
    {
        SKEncodedImageFormat.Jpeg => pixmap.Encode(
            new SKJpegEncoderOptions(Quality, SKJpegEncoderDownsample.Downsample420, SKJpegEncoderAlphaOption.Ignore)),
        SKEncodedImageFormat.Png => pixmap.Encode(new SKPngEncoderOptions(SKPngEncoderFilterFlags.AllFilters, PngCompressionLevel)),
        SKEncodedImageFormat.Webp => pixmap.Encode(new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossy, Quality)),
        _ => null,
    };
}
