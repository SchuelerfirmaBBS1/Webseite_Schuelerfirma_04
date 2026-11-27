using Schuelerfirma.Api.Contracts.Images;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Contracts.Products;

/// <summary>Maps the images of a product (<see cref="ProductImage"/> with its image loaded) to the responses.</summary>
public static class ProductImageResponses
{
    /// <summary>URL of the file for visitors.</summary>
    public static string PublicUrl(int productId, int imageId) => $"/api/public/products/{productId}/images/{imageId}/file";

    /// <summary>URL of the file for the admin area.</summary>
    public static string AdminUrl(int productId, int imageId) => $"/api/admin/products/{productId}/images/{imageId}/file";

    /// <summary>The image as the admin area sees it.</summary>
    public static ImageResponse ForAdmin(ProductImage assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        var image = LoadedImage(assignment);
        return ImageResponse.Create(
            image, assignment.SortOrder, assignment.IsPrimary, AdminUrl(assignment.ProductId, assignment.ImageId));
    }

    /// <summary>The image as visitors see it.</summary>
    public static PublicImageResponse ForPublic(ProductImage assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        var image = LoadedImage(assignment);
        return PublicImageResponse.Create(image, assignment.IsPrimary, PublicUrl(assignment.ProductId, assignment.ImageId));
    }

    private static Image LoadedImage(ProductImage assignment) =>
        assignment.Image ?? throw new InvalidOperationException($"Image {assignment.ImageId} of the product is not loaded.");
}
