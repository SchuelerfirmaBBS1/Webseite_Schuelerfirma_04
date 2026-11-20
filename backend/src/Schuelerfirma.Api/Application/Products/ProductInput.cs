namespace Schuelerfirma.Api.Application.Products;

/// <summary>The editable fields of a product, as the admin area sends them.</summary>
/// <param name="CategoryId">An existing category.</param>
/// <param name="Name">Name; leading and trailing spaces are removed.</param>
/// <param name="Description">Optional description; an empty text is stored as <c>null</c>.</param>
/// <param name="Price">Price in euros, or <c>null</c> if there is none.</param>
/// <param name="IsPriceVisible">Whether visitors see the price.</param>
/// <param name="IsActive">Whether visitors see the product.</param>
public sealed record ProductInput(
    int CategoryId,
    string Name,
    string? Description,
    decimal? Price,
    bool IsPriceVisible,
    bool IsActive);
