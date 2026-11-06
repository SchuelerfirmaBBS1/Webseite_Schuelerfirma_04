using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Schuelerfirma.Api.Infrastructure.Http;

/// <summary>
/// Checks the CSRF token of every POST, PUT, PATCH and DELETE that reaches a controller, including anonymous ones
/// such as the login. Endpoints marked with <c>[IgnoreAntiforgeryToken]</c> are skipped.
/// </summary>
/// <remarks>
/// Replaces the framework's <c>AutoValidateAntiforgeryToken</c>, which answers with a bare "Bad Request". Here the
/// ProblemDetails says what is wrong, so the frontend can tell a missing token apart from invalid input.
/// Authorization runs earlier (middleware), so a request without login gets its 401 before this check.
/// </remarks>
public sealed class CsrfValidationFilter(
    IAntiforgery antiforgery,
    ProblemDetailsFactory problemDetails,
    ILogger<CsrfValidationFilter> logger) : IAsyncAuthorizationFilter
{
    /// <inheritdoc />
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var method = http.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method))
        {
            return;
        }

        if (context.FindEffectivePolicy<IAntiforgeryPolicy>() is IgnoreAntiforgeryTokenAttribute)
        {
            return;
        }

        try
        {
            await antiforgery.ValidateRequestAsync(http);
        }
        catch (AntiforgeryValidationException exception)
        {
            logger.LogInformation("Rejected a request without a valid CSRF token: {Reason}", exception.Message);
            context.Result = new ObjectResult(problemDetails.CreateProblemDetails(
                http,
                StatusCodes.Status400BadRequest,
                title: "Sicherheitstoken fehlt oder ist ungültig",
                detail: "Bitte die Seite neu laden und es erneut versuchen."))
            {
                StatusCode = StatusCodes.Status400BadRequest,
            };
        }
    }
}
