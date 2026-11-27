using Microsoft.AspNetCore.Mvc;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Application.Products;
using Schuelerfirma.Api.Contracts.Products;
using Schuelerfirma.Api.Infrastructure.Auth;
using Schuelerfirma.Api.Infrastructure.Images;

namespace Schuelerfirma.Api.Controllers;

/// <summary>
/// Images of a product in the admin area (#11): upload, edit, delete, and view them also while visitors may not see
/// the product. Every upload is checked and converted (<see cref="IImageProcessor"/>) before it is stored.
/// </summary>
[Route("api/admin/products/{productId:int}/images")]
public class AdminProductImagesController(ProductImageService productImages) : AdminApiController
{
    // The fallback policy and [HasPermission] guarantee a logged-in user here.
    private int ActingUserId => User.GetUserId()!.Value;

    /// <summary>
    /// Uploads one or more images (form field <c>files</c>). Allowed are JPEG, PNG and WebP; they are turned upright,
    /// scaled down and stored without metadata. If one file is rejected, none is stored.
    /// </summary>
    /// <remarks>
    /// No <c>param</c> tags on purpose: with them, the generated OpenAPI document gives the form the description of
    /// the cancellation token.
    /// </remarks>
    /// <response code="201">The new images, appended after the existing ones.</response>
    /// <response code="400">No file, too many files, or a file is not a supported image (message at <c>files</c>).</response>
    /// <response code="404">There is no such product.</response>
    /// <response code="413">The request or a file is too large.</response>
    [HttpPost]
    [HasPermission(Permissions.ProductsManage)]
    [ImageUploadLimits]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<IReadOnlyList<ProductImageResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Upload(
        int productId, IFormFileCollection? files, CancellationToken cancellationToken)
    {
        var uploads = new List<ImageUpload>(files?.Count ?? 0);
        foreach (var file in files ?? Enumerable.Empty<IFormFile>())
        {
            using var stream = new MemoryStream((int)file.Length);
            await file.CopyToAsync(stream, cancellationToken);
            uploads.Add(new ImageUpload(file.FileName, stream.ToArray()));
        }

        var result = await productImages.UploadAsync(ActingUserId, productId, uploads, cancellationToken);
        if (result.Error != CatalogError.None)
        {
            return this.ToActionResult(result);
        }

        IReadOnlyList<ProductImageResponse> created = [.. result.Value!.Select(ProductImageResponse.From)];
        return Created($"/api/admin/products/{productId}", created);
    }

    /// <summary>Changes description, position and main-image flag of an image.</summary>
    /// <response code="200">The changed image.</response>
    /// <response code="400">An input is invalid.</response>
    /// <response code="404">The product has no such image.</response>
    [HttpPut("{imageId:int}")]
    [HasPermission(Permissions.ProductsManage)]
    [ProducesResponseType<ProductImageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int productId, int imageId, UpdateProductImageRequest request, CancellationToken cancellationToken)
    {
        var result = await productImages.UpdateAsync(ActingUserId, productId, imageId, request.ToInput(), cancellationToken);
        return result.Error == CatalogError.None ? Ok(ProductImageResponse.From(result.Value!)) : this.ToActionResult(result);
    }

    /// <summary>Deletes an image together with its data.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="404">The product has no such image.</response>
    [HttpDelete("{imageId:int}")]
    [HasPermission(Permissions.ProductsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int productId, int imageId, CancellationToken cancellationToken)
    {
        var result = await productImages.DeleteAsync(ActingUserId, productId, imageId, cancellationToken);
        return result.Error == CatalogError.None ? NoContent() : this.ToActionResult(result);
    }

    /// <summary>Returns the image file, also of products visitors may not see. Not cached.</summary>
    /// <response code="200">The image.</response>
    /// <response code="404">The product has no such image.</response>
    [HttpGet("{imageId:int}/file")]
    [HasPermission(Permissions.ProductsRead)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "image/jpeg", "image/png", "image/webp")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFile(int productId, int imageId, CancellationToken cancellationToken) =>
        await productImages.GetFileAsync(productId, imageId, cancellationToken) is { } file
            ? File(file.Data, file.ContentType)
            : NotFound();
}
