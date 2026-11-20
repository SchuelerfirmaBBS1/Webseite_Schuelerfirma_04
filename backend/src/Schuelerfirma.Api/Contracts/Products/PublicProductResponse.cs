using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Contracts.Products;

/// <summary>
/// A product as visitors see it. Own class for the public area: it has no internal fields (<c>IsActive</c>,
/// <c>IsPriceVisible</c>, file paths), and a hidden price never leaves the server.
/// </summary>
/// <param name="Id">Id of the product.</param>
/// <param name="CategoryId">Id of the category.</param>
/// <param name="CategoryName">Name of the category.</param>
/// <param name="Name">Name (plain text).</param>
/// <param name="Description">Description (plain text), or <c>null</c>.</param>
/// <param name="Price">Price in euros, or <c>null</c> if there is none or it is hidden.</param>
/// <param name="Images">The images, ordered by <c>SortOrder</c>.</param>
public sealed record PublicProductResponse(
    int Id,
    int CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    decimal? Price,
    IReadOnlyList<PublicProductImageResponse> Images)
{
    /// <summary>Maps a product that visitors may see; replaces a hidden price by <c>null</c>.</summary>
    public static PublicProductResponse From(Product product) => new(
        product.ProductId,
        product.CategoryId,
        product.Category?.Name ?? string.Empty,
        product.Name,
        product.Description,
        product.IsPriceVisible ? product.Price : null,
        [.. product.Images.Select(image => PublicProductImageResponse.From(product.ProductId, image))]);
}

/// <summary>An image of a product as visitors see it.</summary>
/// <param name="Url">Where the file can be downloaded (relative to the website).</param>
/// <param name="Description">Description (alternative text), or <c>null</c>.</param>
/// <param name="IsPrimary">Whether this is the main image.</param>
public sealed record PublicProductImageResponse(string Url, string? Description, bool IsPrimary)
{
    /// <summary>Maps an image of the product with id <paramref name="productId"/>.</summary>
    public static PublicProductImageResponse From(int productId, ProductImage image) => new(
        $"/api/public/products/{productId}/images/{image.ProductImageId}/file", image.Description, image.IsPrimary);
}

/// <summary>A category as visitors see it.</summary>
/// <param name="Id">Id of the category (for <c>?categoryId=</c>).</param>
/// <param name="Name">Name (plain text).</param>
public sealed record PublicCategoryResponse(int Id, string Name)
{
    /// <summary>Maps a visible category.</summary>
    public static PublicCategoryResponse From(Category category) => new(category.CategoryId, category.Name);
}
