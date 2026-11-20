using System.ComponentModel.DataAnnotations;
using Schuelerfirma.Api.Application.Products;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Contracts.Products;

/// <summary>Body of <c>POST /api/admin/products</c>. Texts are plain text, never HTML.</summary>
/// <param name="CategoryId">Id of an existing category.</param>
/// <param name="Name">Name, at most 200 characters.</param>
/// <param name="Description">Optional description, at most 4000 characters.</param>
/// <param name="Price">Price in euros (at least 0, at most two decimal places), or <c>null</c>.</param>
/// <param name="IsPriceVisible">Whether visitors see the price; default <c>true</c>.</param>
/// <param name="IsActive">Whether visitors see the product; default <c>true</c>.</param>
public sealed record CreateProductRequest(
    [Required(ErrorMessage = "Bitte eine Kategorie angeben.")] int? CategoryId,
    [Required(ErrorMessage = "Bitte einen Namen angeben."), StringLength(Product.NameMaxLength, ErrorMessage = "Der Name darf höchstens {1} Zeichen lang sein.")] string Name,
    [StringLength(Product.DescriptionMaxLength, ErrorMessage = "Die Beschreibung darf höchstens {1} Zeichen lang sein.")] string? Description,
    [ValidPrice] decimal? Price,
    bool IsPriceVisible = true,
    bool IsActive = true)
{
    /// <summary>The fields for <see cref="ProductCatalogService"/>.</summary>
    public ProductInput ToInput() => new(CategoryId!.Value, Name, Description, Price, IsPriceVisible, IsActive);
}

/// <summary>Body of <c>PUT /api/admin/products/{id}</c>; all fields are replaced. Texts are plain text, never HTML.</summary>
/// <param name="CategoryId">Id of an existing category.</param>
/// <param name="Name">Name, at most 200 characters.</param>
/// <param name="Description">Optional description, at most 4000 characters; <c>null</c> removes it.</param>
/// <param name="Price">Price in euros (at least 0, at most two decimal places); <c>null</c> removes it.</param>
/// <param name="IsPriceVisible">Whether visitors see the price.</param>
/// <param name="IsActive">Whether visitors see the product.</param>
public sealed record UpdateProductRequest(
    [Required(ErrorMessage = "Bitte eine Kategorie angeben.")] int? CategoryId,
    [Required(ErrorMessage = "Bitte einen Namen angeben."), StringLength(Product.NameMaxLength, ErrorMessage = "Der Name darf höchstens {1} Zeichen lang sein.")] string Name,
    [StringLength(Product.DescriptionMaxLength, ErrorMessage = "Die Beschreibung darf höchstens {1} Zeichen lang sein.")] string? Description,
    [ValidPrice] decimal? Price,
    [Required(ErrorMessage = "Bitte angeben, ob der Preis sichtbar ist.")] bool? IsPriceVisible,
    [Required(ErrorMessage = "Bitte angeben, ob das Produkt aktiv ist.")] bool? IsActive)
{
    /// <summary>The fields for <see cref="ProductCatalogService"/>.</summary>
    public ProductInput ToInput() =>
        new(CategoryId!.Value, Name, Description, Price, IsPriceVisible!.Value, IsActive!.Value);
}

/// <summary>Body of <c>PUT /api/admin/products/{id}/price-visibility</c>.</summary>
/// <param name="IsPriceVisible">Whether visitors see the price.</param>
public sealed record PriceVisibilityRequest(
    [Required(ErrorMessage = "Bitte angeben, ob der Preis sichtbar ist.")] bool? IsPriceVisible);

/// <summary>Body of <c>POST /api/admin/categories</c> and <c>PUT /api/admin/categories/{id}</c>.</summary>
/// <param name="Name">Unique name (case-insensitive), at most 100 characters; plain text.</param>
/// <param name="IsVisible">Whether visitors see the category and its products.</param>
public sealed record SaveCategoryRequest(
    [Required(ErrorMessage = "Bitte einen Namen angeben."), StringLength(Category.NameMaxLength, ErrorMessage = "Der Name darf höchstens {1} Zeichen lang sein.")] string Name,
    [Required(ErrorMessage = "Bitte angeben, ob die Kategorie sichtbar ist.")] bool? IsVisible);
