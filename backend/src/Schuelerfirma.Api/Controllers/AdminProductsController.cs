using Microsoft.AspNetCore.Mvc;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Application.Products;
using Schuelerfirma.Api.Contracts.Products;
using Schuelerfirma.Api.Infrastructure.Auth;

namespace Schuelerfirma.Api.Controllers;

/// <summary>
/// Products in the admin area (basis for the admin panel, #11): all products with the real price and all flags.
/// Showing or hiding the price has its own endpoint and permission, so a role can be allowed exactly that.
/// </summary>
[Route("api/admin/products")]
public class AdminProductsController(ProductCatalogService catalog) : AdminApiController
{
    // The fallback policy and [HasPermission] guarantee a logged-in user here.
    private int ActingUserId => User.GetUserId()!.Value;

    /// <summary>Returns all products, optionally of one category, ordered by name.</summary>
    /// <param name="categoryId">Only products of this category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The products.</response>
    [HttpGet]
    [HasPermission(Permissions.ProductsRead)]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<ProductResponse>> GetAll([FromQuery] int? categoryId, CancellationToken cancellationToken) =>
        [.. (await catalog.GetProductsAsync(categoryId, cancellationToken)).Select(ProductResponse.From)];

    /// <summary>Returns one product.</summary>
    /// <response code="200">The product.</response>
    /// <response code="404">There is no such product.</response>
    [HttpGet("{id:int}")]
    [HasPermission(Permissions.ProductsRead)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> Get(int id, CancellationToken cancellationToken) =>
        await catalog.GetProductAsync(id, cancellationToken) is { } product ? ProductResponse.From(product) : NotFound();

    /// <summary>Creates a product. Without the two flags it is active and shows its price.</summary>
    /// <response code="201">The new product.</response>
    /// <response code="400">An input is invalid, e.g. the category does not exist.</response>
    [HttpPost]
    [HasPermission(Permissions.ProductsManage)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await catalog.CreateProductAsync(ActingUserId, request.ToInput(), cancellationToken);
        if (result.Error != CatalogError.None)
        {
            return this.ToActionResult(result);
        }

        var product = ProductResponse.From(result.Value!);
        return CreatedAtAction(nameof(Get), new { id = product.ProductId }, product);
    }

    /// <summary>Replaces all fields of a product; its images stay as they are.</summary>
    /// <response code="200">The changed product.</response>
    /// <response code="400">An input is invalid, e.g. the category does not exist.</response>
    /// <response code="404">There is no such product.</response>
    [HttpPut("{id:int}")]
    [HasPermission(Permissions.ProductsManage)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var result = await catalog.UpdateProductAsync(ActingUserId, id, request.ToInput(), cancellationToken);
        return result.Error == CatalogError.None ? Ok(ProductResponse.From(result.Value!)) : this.ToActionResult(result);
    }

    /// <summary>Shows or hides the price for visitors. The only change the role User may make to a product.</summary>
    /// <response code="204">Saved.</response>
    /// <response code="400">An input is invalid.</response>
    /// <response code="404">There is no such product.</response>
    [HttpPut("{id:int}/price-visibility")]
    [HasPermission(Permissions.ProductsTogglePriceVisibility)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPriceVisibility(int id, PriceVisibilityRequest request, CancellationToken cancellationToken)
    {
        var result = await catalog.SetPriceVisibilityAsync(ActingUserId, id, request.IsPriceVisible!.Value, cancellationToken);
        return result.Error == CatalogError.None ? NoContent() : this.ToActionResult(result);
    }

    /// <summary>Deletes a product together with its images.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="404">There is no such product.</response>
    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.ProductsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await catalog.DeleteProductAsync(ActingUserId, id, cancellationToken);
        return result.Error == CatalogError.None ? NoContent() : this.ToActionResult(result);
    }
}
