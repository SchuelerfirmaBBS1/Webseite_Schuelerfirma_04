using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Schuelerfirma.Api.Infrastructure.Auth;

namespace Schuelerfirma.Api.Infrastructure.OpenApi;

/// <summary>
/// Groups every operation by its API area and adds the answers that do not come from the action itself but from
/// middleware and filters, derived from the endpoint metadata:
/// <list type="bullet">
/// <item>401 for every endpoint without <c>[AllowAnonymous]</c> (fallback policy),</item>
/// <item>403 for endpoints with <c>[HasPermission]</c>,</item>
/// <item>429 for endpoints with <c>[EnableRateLimiting]</c>,</item>
/// <item>400 for writing endpoints (CSRF check).</item>
/// </list>
/// Answers an action documents itself are kept; for a 400 the CSRF case is added to its description. Error answers
/// are listed as <c>application/problem+json</c>, which is what the API actually sends.
/// </summary>
public sealed class ApiAreaOperationTransformer : IOpenApiOperationTransformer
{
    private const string ProblemContentType = "application/problem+json";

    private const string CsrfDescription = "The CSRF token is missing or invalid; call `GET /api/auth/csrf` first.";

    private static readonly string[] ProblemSchemas = [nameof(ProblemDetails), nameof(ValidationProblemDetails)];

    /// <inheritdoc />
    public async Task TransformAsync(
        OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var description = context.Description;
        var path = "/" + (description.RelativePath ?? string.Empty).TrimStart('/');
        if (ApiAreas.Find(path) is { } area)
        {
            operation.Tags = new HashSet<OpenApiTagReference> { new(area.Tag, context.Document) };
        }

        var metadata = description.ActionDescriptor.EndpointMetadata;
        operation.Responses ??= [];
        UseProblemContentType(operation.Responses);

        if (!metadata.OfType<IAllowAnonymous>().Any())
        {
            await AddProblemAsync(operation.Responses, context, StatusCodes.Status401Unauthorized,
                "Not logged in, or the session has expired.", cancellationToken);
        }

        if (metadata.OfType<HasPermissionAttribute>().Any())
        {
            await AddProblemAsync(operation.Responses, context, StatusCodes.Status403Forbidden,
                "Logged in, but the role lacks the required permission, or the temporary password has not been changed yet.",
                cancellationToken);
        }

        if (metadata.OfType<EnableRateLimitingAttribute>().Any())
        {
            await AddProblemAsync(operation.Responses, context, StatusCodes.Status429TooManyRequests,
                "Too many requests from this client; see `Retry-After`.", cancellationToken);
        }

        var method = description.HttpMethod ?? string.Empty;
        var writing = !HttpMethods.IsGet(method) && !HttpMethods.IsHead(method)
            && !HttpMethods.IsOptions(method) && !HttpMethods.IsTrace(method);
        if (writing && metadata.OfType<IAntiforgeryPolicy>().LastOrDefault() is not IgnoreAntiforgeryTokenAttribute)
        {
            if (operation.Responses.TryGetValue(Key(StatusCodes.Status400BadRequest), out var existing)
                && existing is OpenApiResponse badRequest)
            {
                badRequest.Description = $"{badRequest.Description} Or: {CsrfDescription}";
            }
            else
            {
                await AddProblemAsync(operation.Responses, context, StatusCodes.Status400BadRequest, CsrfDescription, cancellationToken);
            }
        }
    }

    // [ProducesResponseType<ProblemDetails>] lists the formatters' JSON types; the API answers errors as problem+json.
    private static void UseProblemContentType(OpenApiResponses responses)
    {
        foreach (var response in responses.Values.OfType<OpenApiResponse>())
        {
            var problem = response.Content?.Values.FirstOrDefault(media =>
                media.Schema is OpenApiSchemaReference reference && ProblemSchemas.Contains(reference.Reference.Id));
            if (problem is not null)
            {
                response.Content = new Dictionary<string, OpenApiMediaType> { [ProblemContentType] = problem };
            }
        }
    }

    private static async Task AddProblemAsync(
        OpenApiResponses responses,
        OpenApiOperationTransformerContext context,
        int statusCode,
        string description,
        CancellationToken cancellationToken)
    {
        if (responses.ContainsKey(Key(statusCode)))
        {
            return;
        }

        // A reference when the schema is already a component (as soon as one action declares ProblemDetails), so the
        // document does not repeat it for every answer.
        IOpenApiSchema schema = context.Document?.Components?.Schemas?.ContainsKey(nameof(ProblemDetails)) == true
            ? new OpenApiSchemaReference(nameof(ProblemDetails), context.Document)
            : await context.GetOrCreateSchemaAsync(typeof(ProblemDetails), null, cancellationToken);

        responses[Key(statusCode)] = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType> { [ProblemContentType] = new() { Schema = schema } },
        };
    }

    private static string Key(int statusCode) => statusCode.ToString(CultureInfo.InvariantCulture);
}
