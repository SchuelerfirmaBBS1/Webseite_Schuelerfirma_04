using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Contracts.Images;

/// <summary>An image as the admin area sees it.</summary>
/// <param name="ImageId">Id of the image.</param>
/// <param name="FileName">Original file name of the upload; plain text, only for display.</param>
/// <param name="Description">Description (alternative text), or <c>null</c>.</param>
/// <param name="ContentType">Media type of the stored image.</param>
/// <param name="Length">Size of the stored image in bytes.</param>
/// <param name="SortOrder">Position in the list of its owner; lower values come first.</param>
/// <param name="IsPrimary">Whether this is the main image of its owner.</param>
/// <param name="Url">Where the admin area loads the image, also while visitors may not see the owner (needs a login).</param>
public sealed record ImageResponse(
    int ImageId,
    string FileName,
    string? Description,
    string ContentType,
    long Length,
    int SortOrder,
    bool IsPrimary,
    string Url)
{
    /// <summary>Maps an image.</summary>
    public static ImageResponse From(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        return new(
            image.ImageId,
            image.FileName,
            image.Description,
            image.ContentType,
            image.Length,
            image.SortOrder,
            image.IsPrimary,
            ImageUrls.Admin(image));
    }
}

/// <summary>
/// An image as visitors see it. Own class for the public area: no file name, size or other internal field.
/// </summary>
/// <param name="Url">Where the file can be downloaded (relative to the website).</param>
/// <param name="Description">Description (alternative text), or <c>null</c>.</param>
/// <param name="IsPrimary">Whether this is the main image of its owner.</param>
public sealed record PublicImageResponse(string Url, string? Description, bool IsPrimary)
{
    /// <summary>Maps an image.</summary>
    public static PublicImageResponse From(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        return new(ImageUrls.Public(image), image.Description, image.IsPrimary);
    }
}

/// <summary>
/// The URLs of an image file. They contain the owner, so the endpoint can check that visitors may see it.
/// </summary>
public static class ImageUrls
{
    /// <summary>URL for visitors.</summary>
    public static string Public(Image image) => $"/api/public/{OwnerPath(image)}/images/{image.ImageId}/file";

    /// <summary>URL for the admin area.</summary>
    public static string Admin(Image image) => $"/api/admin/{OwnerPath(image)}/images/{image.ImageId}/file";

    private static string OwnerPath(Image image) => image.ProductId is { } productId
        ? $"products/{productId}"
        : throw new InvalidOperationException($"Image {image.ImageId} has no owner.");
}
