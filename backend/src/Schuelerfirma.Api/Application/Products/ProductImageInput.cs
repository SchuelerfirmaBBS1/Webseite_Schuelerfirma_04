namespace Schuelerfirma.Api.Application.Products;

/// <summary>The editable fields of a product image.</summary>
/// <param name="Description">Description (alternative text), plain text; blank means none.</param>
/// <param name="SortOrder">Position in the list (0 or more); lower values come first.</param>
/// <param name="IsPrimary">Whether this becomes the main image; the other images of the product then lose the flag.</param>
public sealed record ProductImageInput(string? Description, int SortOrder, bool IsPrimary);
