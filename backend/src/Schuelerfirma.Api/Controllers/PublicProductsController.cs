using Microsoft.AspNetCore.Mvc;
using Schuelerfirma.Api.Application.Products;
using Schuelerfirma.Api.Contracts.Products;

namespace Schuelerfirma.Api.Controllers;

/// <summary>
/// Products for visitors of the website (product page, #23). Only active products in visible categories; a hidden
/// price is <c>null</c>. The image files follow together with the image storage (#13).
/// </summary>
[Route("api/public/products")]
public class PublicProductsController(ProductCatalogService catalog) : PublicApiController
{
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
}
