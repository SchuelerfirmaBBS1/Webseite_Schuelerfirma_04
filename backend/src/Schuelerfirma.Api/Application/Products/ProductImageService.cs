using Microsoft.Extensions.Options;
using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Products;

/// <summary>
/// Images of products: upload, edit and delete in the admin area, deliver to visitors. Controllers only translate
/// into HTTP; checks, rules and log entries live here.
/// </summary>
/// <remarks>
/// Every uploaded file goes through <see cref="IImageProcessor"/>, and only its result is stored. An upload with
/// several files is checked completely first: if one file is rejected, none is stored.
/// </remarks>
public sealed class ProductImageService(
    IProductRepository products,
    IImageRepository images,
    IImageProcessor processor,
    IOptions<ImageUploadOptions> options,
    ILogger<ProductImageService> logger)
{
    /// <summary>Name of the form field with the files; also the field of error messages about them.</summary>
    public const string FilesField = "files";

    /// <summary>Name used when the client sends no usable file name.</summary>
    public const string DefaultFileName = "bild";

    private readonly ImageUploadOptions settings = options.Value;

    /// <summary>
    /// Checks, converts and stores <paramref name="files"/> as new images of the product, appended after the existing
    /// ones. If the product has no main image yet, the first new image becomes it.
    /// </summary>
    /// <param name="actingUserId">The user who does this (for the log).</param>
    /// <param name="productId">The product.</param>
    /// <param name="files">The uploaded files.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new images; invalid at <see cref="FilesField"/> if a file is rejected, not found for an unknown product.</returns>
    public async Task<CatalogResult<IReadOnlyList<Image>>> UploadAsync(
        int actingUserId, int productId, IReadOnlyList<ImageUpload> files, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);

        var product = await products.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return CatalogResult<IReadOnlyList<Image>>.NotFound;
        }

        if (files.Count == 0)
        {
            return CatalogResult<IReadOnlyList<Image>>.Invalid(FilesField, "Bitte mindestens ein Bild auswählen.");
        }

        if (files.Count > settings.MaxFilesPerRequest)
        {
            return CatalogResult<IReadOnlyList<Image>>.Invalid(
                FilesField, $"Es können höchstens {settings.MaxFilesPerRequest} Bilder auf einmal hochgeladen werden.");
        }

        var accepted = new List<(string FileName, ProcessedImage Image)>(files.Count);
        foreach (var file in files)
        {
            var fileName = CleanFileName(file.FileName);
            var result = processor.Process(file.Data);
            if (result.Image is null)
            {
                logger.LogInformation(
                    "User {ActorId} uploaded a rejected image {FileName} for product {ProductId}: {Rejection}",
                    actingUserId, LogText.Safe(fileName), productId, result.Rejection);
                return CatalogResult<IReadOnlyList<Image>>.Invalid(FilesField, RejectionMessage(fileName, result.Rejection));
            }

            accepted.Add((fileName, result.Image));
        }

        var nextSortOrder = product.Images.Count == 0 ? 0 : product.Images.Max(i => i.SortOrder) + 1;
        var needsMainImage = !product.Images.Any(i => i.IsPrimary);
        var saved = new List<Image>(accepted.Count);
        foreach (var (fileName, image) in accepted)
        {
            var record = new Image
            {
                ProductId = productId,
                FileName = fileName,
                ContentType = image.ContentType,
                Length = image.Data.Length,
                SortOrder = nextSortOrder++,
                IsPrimary = needsMainImage && saved.Count == 0,
            };

            try
            {
                await images.AddAsync(record, image.Data, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // The product was deleted in the meantime.
                return CatalogResult<IReadOnlyList<Image>>.NotFound;
            }

            saved.Add(record);
            logger.LogInformation(
                "User {ActorId} uploaded image {ImageId} ({FileName}, {Length} bytes) to product {ProductId}",
                actingUserId, record.ImageId, LogText.Safe(fileName), record.Length, productId);
        }

        return CatalogResult<IReadOnlyList<Image>>.Success(saved);
    }

    /// <summary>Changes description, position and main-image flag of an image.</summary>
    /// <returns>The changed image; invalid with the field name, or not found.</returns>
    public async Task<CatalogResult<Image>> UpdateAsync(
        int actingUserId, int productId, int imageId, ImageInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var description = ProductRules.NormalizeOptional(input.Description);
        if (description?.Length > Image.DescriptionMaxLength)
        {
            return CatalogResult<Image>.Invalid(
                nameof(ImageInput.Description),
                $"Die Beschreibung darf höchstens {Image.DescriptionMaxLength} Zeichen lang sein.");
        }

        if (input.SortOrder < 0)
        {
            return CatalogResult<Image>.Invalid(nameof(ImageInput.SortOrder), "Die Reihenfolge darf nicht negativ sein.");
        }

        var image = new Image
        {
            ProductId = productId,
            ImageId = imageId,
            Description = description,
            SortOrder = input.SortOrder,
            IsPrimary = input.IsPrimary,
        };
        if (!await images.UpdateAsync(image, cancellationToken))
        {
            return CatalogResult<Image>.NotFound;
        }

        logger.LogInformation(
            "User {ActorId} changed image {ImageId} of product {ProductId} (position {SortOrder}, main image {IsPrimary})",
            actingUserId, imageId, productId, input.SortOrder, input.IsPrimary);
        var saved = await images.GetAsync(ImageOwner.Product(productId), imageId, cancellationToken);
        return saved is null ? CatalogResult<Image>.NotFound : CatalogResult<Image>.Success(saved);
    }

    /// <summary>Deletes an image with its data. If it was the main image, the product has none until one is chosen.</summary>
    /// <returns>The deleted image as it was, or not found.</returns>
    public async Task<CatalogResult<Image>> DeleteAsync(
        int actingUserId, int productId, int imageId, CancellationToken cancellationToken = default)
    {
        var image = await images.GetAsync(ImageOwner.Product(productId), imageId, cancellationToken);
        if (image is null || !await images.DeleteAsync(ImageOwner.Product(productId), imageId, cancellationToken))
        {
            return CatalogResult<Image>.NotFound;
        }

        logger.LogInformation(
            "User {ActorId} deleted image {ImageId} ({FileName}) of product {ProductId}",
            actingUserId, imageId, LogText.Safe(image.FileName), productId);
        return CatalogResult<Image>.Success(image);
    }

    /// <summary>The image file for the admin area, also of inactive products; <c>null</c> if unknown.</summary>
    public Task<ImageFile?> GetFileAsync(int productId, int imageId, CancellationToken cancellationToken = default) =>
        images.GetFileAsync(ImageOwner.Product(productId), imageId, cancellationToken);

    /// <summary>The image file for visitors; <c>null</c> if unknown or if visitors may not see the product.</summary>
    public Task<ImageFile?> GetVisibleFileAsync(int productId, int imageId, CancellationToken cancellationToken = default) =>
        images.GetVisibleFileAsync(ImageOwner.Product(productId), imageId, cancellationToken);

    /// <summary>
    /// The part after the last slash or backslash, without control characters, trimmed and shortened to
    /// <see cref="Image.FileNameMaxLength"/>; <see cref="DefaultFileName"/> if nothing is left.
    /// </summary>
    public static string CleanFileName(string? fileName)
    {
        var name = fileName ?? string.Empty;
        name = name[(name.LastIndexOfAny(['/', '\\']) + 1)..];
        name = new string([.. name.Where(c => !char.IsControl(c))]).Trim();
        if (name.Length > Image.FileNameMaxLength)
        {
            name = name[..Image.FileNameMaxLength];
        }

        return name.Length == 0 ? DefaultFileName : name;
    }

    private string RejectionMessage(string fileName, ImageRejection rejection) => rejection switch
    {
        ImageRejection.UnsupportedFormat => $"„{fileName}“ ist kein unterstütztes Bild. Erlaubt sind JPEG, PNG und WebP.",
        ImageRejection.TooManyPixels =>
            $"„{fileName}“ hat zu viele Pixel (höchstens {settings.MaxSourcePixels / 1_000_000.0:0.#} Megapixel).",
        ImageRejection.TooLarge => $"„{fileName}“ ist auch nach dem Verkleinern noch zu groß.",
        _ => $"„{fileName}“ ist beschädigt oder unvollständig.",
    };
}
