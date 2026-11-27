using Microsoft.AspNetCore.Mvc;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Application.Products;
using Schuelerfirma.Api.Contracts.Products;
using Schuelerfirma.Api.Infrastructure.Auth;

namespace Schuelerfirma.Api.Controllers;

/// <summary>Product categories in the admin area. Names are unique (case-insensitive).</summary>
[Route("api/admin/categories")]
public class AdminCategoriesController(ProductCatalogService catalog) : AdminApiController
{
    // The fallback policy and [HasPermission] guarantee a logged-in user here.
    private int ActingUserId => User.GetUserId()!.Value;

    /// <summary>Returns all categories, including hidden ones, ordered by name.</summary>
    /// <response code="200">The categories.</response>
    [HttpGet]
    [HasPermission(Permissions.ProductsRead)]
    [ProducesResponseType<IReadOnlyList<CategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<CategoryResponse>> GetAll(CancellationToken cancellationToken) =>
        [.. (await catalog.GetCategoriesAsync(cancellationToken)).Select(CategoryResponse.From)];

    /// <summary>Creates a category.</summary>
    /// <response code="201">The new category.</response>
    /// <response code="400">An input is invalid.</response>
    /// <response code="409">There is already a category with this name.</response>
    [HttpPost]
    [HasPermission(Permissions.ProductsManage)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(SaveCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await catalog.CreateCategoryAsync(ActingUserId, request.Name, request.IsVisible!.Value, cancellationToken);
        if (result.Error != CatalogError.None)
        {
            return this.ToActionResult(result);
        }

        var category = CategoryResponse.From(result.Value!);
        return Created($"/api/admin/categories/{category.CategoryId}", category);
    }

    /// <summary>Renames a category and shows or hides it (with its products).</summary>
    /// <response code="200">The changed category.</response>
    /// <response code="400">An input is invalid.</response>
    /// <response code="404">There is no such category.</response>
    /// <response code="409">There is already another category with this name.</response>
    [HttpPut("{id:int}")]
    [HasPermission(Permissions.ProductsManage)]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, SaveCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await catalog.UpdateCategoryAsync(ActingUserId, id, request.Name, request.IsVisible!.Value, cancellationToken);
        return result.Error == CatalogError.None ? Ok(CategoryResponse.From(result.Value!)) : this.ToActionResult(result);
    }

    /// <summary>Deletes a category that contains no products.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="404">There is no such category.</response>
    /// <response code="409">The category still contains products.</response>
    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.ProductsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await catalog.DeleteCategoryAsync(ActingUserId, id, cancellationToken);
        return result.Error == CatalogError.None ? NoContent() : this.ToActionResult(result);
    }
}
