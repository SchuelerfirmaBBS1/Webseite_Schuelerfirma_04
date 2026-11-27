using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Application.Products;

namespace Schuelerfirma.Api.Tests.Images;

public class ImageUploadRulesTests
{
    [Theory]
    [InlineData("foto.jpg", "foto.jpg")]
    [InlineData("../../etc/passwd.jpg", "passwd.jpg")]
    [InlineData(@"C:\Users\anna\Bilder\urlaub.png", "urlaub.png")]
    [InlineData("  zeile\numbruch.jpg  ", "zeileumbruch.jpg")]
    [InlineData("../", "bild")]
    [InlineData("", "bild")]
    [InlineData(null, "bild")]
    public void FileNames_AreOnlyKeptAsText_WithoutPathOrControlCharacters(string? sent, string stored)
    {
        Assert.Equal(stored, ProductImageService.CleanFileName(sent));
    }

    [Fact]
    public void LongFileNames_AreShortened()
    {
        Assert.Equal(255, ProductImageService.CleanFileName(new string('a', 400) + ".jpg").Length);
    }

    private static ValidateOptionsResult Validate(ImageUploadOptions options) =>
        new DataAnnotationValidateOptions<ImageUploadOptions>(null).Validate(null, options);

    [Fact]
    public void DefaultOptions_AreValid()
    {
        Assert.True(Validate(new ImageUploadOptions()).Succeeded);
    }

    [Fact]
    public void RequestLimit_BelowTheFileLimit_IsRejected()
    {
        var result = Validate(new ImageUploadOptions { MaxFileSizeBytes = 2_000, MaxRequestSizeBytes = 1_000 });

        Assert.True(result.Failed);
        Assert.Contains("MaxRequestSizeBytes", result.FailureMessage);
    }

    [Fact]
    public void FileLimit_AboveTheColumnSize_IsRejected()
    {
        var result = Validate(new ImageUploadOptions { MaxFileSizeBytes = 20 * 1024 * 1024, MaxRequestSizeBytes = 40 * 1024 * 1024 });

        Assert.True(result.Failed);
        Assert.Contains("MaxFileSizeBytes", result.FailureMessage);
    }
}
