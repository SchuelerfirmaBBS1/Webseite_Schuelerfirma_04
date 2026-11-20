using Microsoft.AspNetCore.Mvc;
using Schuelerfirma.Api.Application.Products;
using Schuelerfirma.Api.Contracts.Products;

namespace Schuelerfirma.Api.Controllers;

/// <summary>Product categories for visitors of the website, e.g. for a filter on the product page.</summary>
[Route("api/public/categories")]
public class PublicCategoriesController(ProductCatalogService catalog) : PublicApiController
{
    /// <summary>Returns the visible categories, ordered by name.</summary>
    /// <response code="200">The categories.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PublicCategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<PublicCategoryResponse>> GetAll(CancellationToken cancellationToken) =>
        [.. (await catalog.GetVisibleCategoriesAsync(cancellationToken)).Select(PublicCategoryResponse.From)];
}
