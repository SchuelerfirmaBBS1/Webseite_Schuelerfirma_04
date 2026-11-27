using System.ComponentModel.DataAnnotations;
using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Contracts.Images;

/// <summary>Body of <c>PUT …/images/{imageId}</c>; all fields are replaced.</summary>
/// <param name="Description">Description (alternative text), at most 500 characters, plain text; <c>null</c> removes it.</param>
/// <param name="SortOrder">Position in the list (0 or more); lower values come first.</param>
/// <param name="IsPrimary">Whether this is the main image; making it the main image takes the flag from the others.</param>
public sealed record UpdateImageRequest(
    [StringLength(Image.DescriptionMaxLength, ErrorMessage = "Die Beschreibung darf höchstens {1} Zeichen lang sein.")] string? Description,
    [Required(ErrorMessage = "Bitte eine Reihenfolge angeben."), Range(0, int.MaxValue, ErrorMessage = "Die Reihenfolge darf nicht negativ sein.")] int? SortOrder,
    [Required(ErrorMessage = "Bitte angeben, ob es das Hauptbild ist.")] bool? IsPrimary)
{
    /// <summary>The editable fields.</summary>
    public ImageInput ToInput() => new(Description, SortOrder!.Value, IsPrimary!.Value);
}
