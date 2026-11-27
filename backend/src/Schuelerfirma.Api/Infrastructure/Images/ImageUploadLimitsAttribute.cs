using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Application.Products;

namespace Schuelerfirma.Api.Infrastructure.Images;

/// <summary>
/// Applies the upload limits from <see cref="ImageUploadOptions"/> to an endpoint that receives images as
/// <c>multipart/form-data</c>, and answers too large requests and files with <c>413</c>.
/// </summary>
/// <remarks>
/// <para>
/// A resource filter, so it runs after authentication, the permission check and the CSRF check (an anonymous or
/// unauthorized request is rejected before its body is read) but before model binding. It reads the form itself with
/// the configured limits; model binding then uses the form that is already read.
/// </para>
/// <para>
/// Without this, the framework's limits would apply (about 28.6 MiB per request) and a too large request would end
/// as <c>400</c> from model binding instead of <c>413</c>.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ImageUploadLimitsAttribute : Attribute, IFilterFactory
{
    /// <inheritdoc />
    public bool IsReusable => true;

    /// <inheritdoc />
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider) =>
        new ImageUploadLimitsFilter(
            serviceProvider.GetRequiredService<IOptions<ImageUploadOptions>>(),
            serviceProvider.GetRequiredService<ProblemDetailsFactory>());

    private sealed class ImageUploadLimitsFilter(IOptions<ImageUploadOptions> options, ProblemDetailsFactory problemDetails)
        : IAsyncResourceFilter
    {
        private readonly ImageUploadOptions settings = options.Value;

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var http = context.HttpContext;
            var request = http.Request;

            // Kestrel stops reading the body at this size (also without a Content-Length header) with 413.
            if (http.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } bodySize)
            {
                bodySize.MaxRequestBodySize = settings.MaxRequestSizeBytes;
            }

            if (request.ContentLength > settings.MaxRequestSizeBytes)
            {
                context.Result = TooLarge(http, $"Die Anfrage ist zu groß (höchstens {Megabytes(settings.MaxRequestSizeBytes)} insgesamt).");
                return;
            }

            // Anything else than a form is left to [Consumes] / model binding (415 or 400).
            if (request.HasFormContentType)
            {
                IFormCollection form;
                try
                {
                    form = await request.ReadFormAsync(
                        new FormOptions { MultipartBodyLengthLimit = settings.MaxRequestSizeBytes },
                        http.RequestAborted);
                }
                catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
                {
                    context.Result = TooLarge(http, $"Die Anfrage ist zu groß (höchstens {Megabytes(settings.MaxRequestSizeBytes)} insgesamt).");
                    return;
                }
                catch (InvalidDataException)
                {
                    context.Result = Problem(http, StatusCodes.Status400BadRequest, "Formular unlesbar", "Das Formular konnte nicht gelesen werden.");
                    return;
                }

                var tooLarge = form.Files.FirstOrDefault(file => file.Length > settings.MaxFileSizeBytes);
                if (tooLarge is not null)
                {
                    var name = ProductImageService.CleanFileName(tooLarge.FileName);
                    context.Result = TooLarge(http, $"„{name}“ ist zu groß (höchstens {Megabytes(settings.MaxFileSizeBytes)} pro Bild).");
                    return;
                }
            }

            await next();
        }

        private ObjectResult TooLarge(HttpContext http, string detail) =>
            Problem(http, StatusCodes.Status413PayloadTooLarge, "Zu groß", detail);

        private ObjectResult Problem(HttpContext http, int status, string title, string detail) =>
            new(problemDetails.CreateProblemDetails(http, status, title: title, detail: detail)) { StatusCode = status };

        private static string Megabytes(long bytes) => $"{bytes / (1024.0 * 1024.0):0.#} MB";
    }
}
