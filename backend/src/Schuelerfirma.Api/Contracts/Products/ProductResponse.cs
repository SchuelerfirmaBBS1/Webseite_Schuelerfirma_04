using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Contracts.Products;

/// <summary>A product as the admin area sees it: with the real price and all flags.</summary>
/// <param name="ProductId">Id of the product.</param>
/// <param name="CategoryId">Id of the category.</param>
/// <param name="CategoryName">Name of the category.</param>
/// <param name="Name">Name.</param>
/// <param name="Description">Description, or <c>null</c>.</param>
/// <param name="Price">The real price, also when it is hidden from visitors; <c>null</c> if there is none.</param>
/// <param name="IsPriceVisible">Whether visitors see the price.</param>
/// <param name="IsActive">Whether visitors see the product.</param>
/// <param name="Images">The images, ordered by <c>SortOrder</c>.</param>
public sealed record ProductResponse(
    int ProductId,
    int CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    decimal? Price,
    bool IsPriceVisible,
    bool IsActive,
    IReadOnlyList<ProductImageResponse> Images)
{
    /// <summary>Maps a product loaded by the repository.</summary>
    public static ProductResponse From(Product product) => new(
        product.ProductId,
        product.CategoryId,
        product.Category?.Name ?? string.Empty,
        product.Name,
        product.Description,
        product.Price,
        product.IsPriceVisible,
        product.IsActive,
        [.. product.Images.Select(ProductImageResponse.From)]);
}

/// <summary>An image of a product as the admin area sees it.</summary>
/// <param name="ProductImageId">Id of the image.</param>
/// <param name="FileName">Original file name of the upload; plain text, only for display.</param>
/// <param name="Description">Description (alternative text), or <c>null</c>.</param>
/// <param name="ContentType">Media type of the stored image.</param>
/// <param name="Length">Size of the stored image in bytes.</param>
/// <param name="SortOrder">Position in the list; lower values come first.</param>
/// <param name="IsPrimary">Whether this is the main image.</param>
/// <param name="Url">Where the admin area loads the image, also for inactive products (needs a login).</param>
public sealed record ProductImageResponse(
    int ProductImageId,
    string FileName,
    string? Description,
    string ContentType,
    long Length,
    int SortOrder,
    bool IsPrimary,
    string Url)
{
    /// <summary>Maps an image.</summary>
    public static ProductImageResponse From(ProductImage image) => new(
        image.ProductImageId,
        image.FileName,
        image.Description,
        image.ContentType,
        image.Length,
        image.SortOrder,
        image.IsPrimary,
        $"/api/admin/products/{image.ProductId}/images/{image.ProductImageId}/file");
}

/// <summary>A category as the admin area sees it.</summary>
/// <param name="CategoryId">Id of the category.</param>
/// <param name="Name">Name.</param>
/// <param name="IsVisible">Whether visitors see the category and its products.</param>
public sealed record CategoryResponse(int CategoryId, string Name, bool IsVisible)
{
    /// <summary>Maps a category.</summary>
    public static CategoryResponse From(Category category) => new(category.CategoryId, category.Name, category.IsVisible);
}
