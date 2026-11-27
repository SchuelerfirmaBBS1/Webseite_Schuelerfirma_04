using Microsoft.AspNetCore.Mvc;
using Schuelerfirma.Api.Application.Products;
using Schuelerfirma.Api.Contracts.Products;

namespace Schuelerfirma.Api.Controllers;

/// <summary>
/// Products for visitors of the website (product page, #23). Only active products in visible categories; a hidden
/// price is <c>null</c>. Image files are delivered only for products visitors may see.
/// </summary>
[Route("api/public/products")]
public class PublicProductsController(ProductCatalogService catalog, ProductImageService productImages) : PublicApiController
{
    /// <summary>
    /// Browsers may keep an image for a year without asking again: an image id never gets another file, a new upload
    /// gets a new id.
    /// </summary>
    public const string ImageCacheControl = "public, max-age=31536000, immutable";

    /// <summary>Returns the visible products, optionally of one category, ordered by name.</summary>
    /// <param name="categoryId">Only products of this category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The products.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PublicProductResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<PublicProductResponse>> GetAll([FromQuery] int? categoryId, CancellationToken cancellationToken) =>
        [.. (await catalog.GetVisibleProductsAsync(categoryId, cancellationToken)).Select(PublicProductResponse.From)];

    /// <summary>Returns one visible product.</summary>
    /// <response code="200">The product.</response>
    /// <response code="404">There is no such product, or visitors may not see it (same answer for both).</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PublicProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicProductResponse>> Get(int id, CancellationToken cancellationToken) =>
        await catalog.GetVisibleProductAsync(id, cancellationToken) is { } product ? PublicProductResponse.From(product) : NotFound();

    /// <summary>Returns an image file of a visible product.</summary>
    /// <param name="productId">The product.</param>
    /// <param name="imageId">The image of that product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The image; cacheable for a year.</response>
    /// <response code="404">Unknown, an image of another product, or visitors may not see the product (same answer).</response>
    [HttpGet("{productId:int}/images/{imageId:int}/file")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "image/jpeg", "image/png", "image/webp")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImageFile(int productId, int imageId, CancellationToken cancellationToken)
    {
        var file = await productImages.GetVisibleFileAsync(productId, imageId, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = ImageCacheControl;
        return File(file.Data, file.ContentType);
    }
}
