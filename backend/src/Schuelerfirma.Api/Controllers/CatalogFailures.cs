using Microsoft.AspNetCore.Mvc;
using Schuelerfirma.Api.Application.Products;

namespace Schuelerfirma.Api.Controllers;

/// <summary>Translates a failed <see cref="CatalogResult{T}"/> into the HTTP answer.</summary>
internal static class CatalogFailures
{
    /// <summary><c>404</c>, <c>400</c> with the message at the field, or <c>409</c> with the message.</summary>
    public static IActionResult ToActionResult<T>(this ControllerBase controller, CatalogResult<T> result)
        where T : class
    {
        switch (result.Error)
        {
            case CatalogError.NotFound:
                return controller.NotFound();

            case CatalogError.Invalid:
                controller.ModelState.AddModelError(result.Field!, result.Message!);
                return controller.ValidationProblem();

            default:
                return controller.Problem(statusCode: StatusCodes.Status409Conflict, title: "Nicht möglich", detail: result.Message);
        }
    }
}
