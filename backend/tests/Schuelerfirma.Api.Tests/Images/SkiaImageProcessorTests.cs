using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Infrastructure.Images;
using SkiaSharp;

namespace Schuelerfirma.Api.Tests.Images;

public class SkiaImageProcessorTests
{
    private static SkiaImageProcessor Processor(Action<ImageUploadOptions>? configure = null)
    {
        var options = new ImageUploadOptions();
        configure?.Invoke(options);
        return new SkiaImageProcessor(Options.Create(options));
    }

    private static SKColor PixelAt(byte[] file, int x, int y)
    {
        using var bitmap = SKBitmap.Decode(file);
        return bitmap.GetPixel(x, y);
    }

    private static bool IsReddish(SKColor color) => color.Red > 200 && color.Blue < 80;

    private static bool IsBluish(SKColor color) => color.Blue > 200 && color.Red < 80;

    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg, "image/jpeg")]
    [InlineData(SKEncodedImageFormat.Png, "image/png")]
    [InlineData(SKEncodedImageFormat.Webp, "image/webp")]
    public void SupportedFormats_AreAccepted_AndKeepTheirFormat(SKEncodedImageFormat format, string contentType)
    {
        var result = Processor().Process(TestImages.Create(format));

        Assert.Equal(ImageRejection.None, result.Rejection);
        Assert.Equal(contentType, result.Image!.ContentType);
        Assert.Equal((40, 20), (result.Image.Width, result.Image.Height));
        using var codec = SKCodec.Create(SKData.CreateCopy(result.Image.Data));
        Assert.Equal(format, codec.EncodedFormat);
    }

    [Fact]
    public void Svg_Html_Gif_AndText_AreRejected_WhateverTheyAreCalled()
    {
        var processor = Processor();

        Assert.Equal(ImageRejection.UnsupportedFormat, processor.Process(TestImages.Svg()).Rejection);
        Assert.Equal(ImageRejection.UnsupportedFormat, processor.Process(TestImages.Html()).Rejection);
        Assert.Equal(ImageRejection.UnsupportedFormat, processor.Process(TestImages.Gif()).Rejection);
        Assert.Equal(ImageRejection.UnsupportedFormat, processor.Process("kein Bild"u8).Rejection);
        Assert.Equal(ImageRejection.UnsupportedFormat, processor.Process([]).Rejection);
    }

    [Fact]
    public void JpegSignature_FollowedByHtml_IsRejected()
    {
        byte[] disguised = [0xFF, 0xD8, 0xFF, 0xE0, .. TestImages.Html()];

        var result = Processor().Process(disguised);

        Assert.NotEqual(ImageRejection.None, result.Rejection);
    }

    [Fact]
    public void TruncatedJpeg_IsCorrupt()
    {
        var jpeg = TestImages.Jpeg(400, 300);

        var result = Processor().Process(jpeg.AsSpan(0, jpeg.Length / 2));

        Assert.Equal(ImageRejection.Corrupt, result.Rejection);
    }

    [Fact]
    public void ExifWithGpsPosition_IsRemoved()
    {
        var withGps = TestImages.WithExif(TestImages.Jpeg());
        Assert.True(TestImages.Contains(withGps, TestImages.SecretMarker));

        var result = Processor().Process(withGps);

        Assert.Equal(ImageRejection.None, result.Rejection);
        Assert.False(TestImages.Contains(result.Image!.Data, "Exif"));
        Assert.False(TestImages.Contains(result.Image.Data, TestImages.SecretMarker));
    }

    [Fact]
    public void PngTextChunk_IsRemoved()
    {
        var withText = TestImages.WithPngText(TestImages.Png());
        Assert.Equal(ImageRejection.None, Processor().Process(TestImages.Png()).Rejection);

        var result = Processor().Process(withText);

        Assert.Equal(ImageRejection.None, result.Rejection);
        Assert.False(TestImages.Contains(result.Image!.Data, TestImages.SecretMarker));
        Assert.False(TestImages.Contains(result.Image.Data, "tEXt"));
    }

    [Fact]
    public void ExifOrientation_IsApplied_AndNotStoredAgain()
    {
        // Stored 40×20 (red left, blue right), EXIF 6: the camera was turned, the picture is meant upright 20×40.
        var turned = TestImages.WithExif(TestImages.Jpeg(), orientation: 6);

        var result = Processor().Process(turned);

        Assert.Equal((20, 40), (result.Image!.Width, result.Image.Height));
        // Turned by 90° clockwise: the left (red) half ends up at the top, the right (blue) half at the bottom.
        Assert.True(IsReddish(PixelAt(result.Image.Data, 10, 5)));
        Assert.True(IsBluish(PixelAt(result.Image.Data, 10, 35)));
        using var codec = SKCodec.Create(SKData.CreateCopy(result.Image.Data));
        Assert.Equal(SKEncodedOrigin.TopLeft, codec.EncodedOrigin);
    }

    [Fact]
    public void LargeImages_AreScaledDown_KeepingTheAspectRatio()
    {
        var result = Processor(o => o.MaxDimension = 200).Process(TestImages.Jpeg(600, 300));

        Assert.Equal((200, 100), (result.Image!.Width, result.Image.Height));
        Assert.True(IsReddish(PixelAt(result.Image.Data, 20, 50)));
        Assert.True(IsBluish(PixelAt(result.Image.Data, 180, 50)));
    }

    [Fact]
    public void SmallImages_AreNotEnlarged()
    {
        var result = Processor().Process(TestImages.Png(30, 10));

        Assert.Equal((30, 10), (result.Image!.Width, result.Image.Height));
    }

    [Fact]
    public void Transparency_OfPngs_IsKept()
    {
        var result = Processor().Process(TestImages.Png(transparent: true));

        Assert.Equal(0, PixelAt(result.Image!.Data, 5, 5).Alpha);
    }

    [Fact]
    public void TooManyPixels_AreRejected_BeforeDecoding()
    {
        var result = Processor(o => o.MaxSourcePixels = 40 * 20 - 1).Process(TestImages.Png());

        Assert.Equal(ImageRejection.TooManyPixels, result.Rejection);
    }

    [Fact]
    public void ProcessedImage_LargerThanTheLimit_IsRejected()
    {
        var result = Processor(o => o.MaxFileSizeBytes = 10).Process(TestImages.Png());

        Assert.Equal(ImageRejection.TooLarge, result.Rejection);
    }
}
