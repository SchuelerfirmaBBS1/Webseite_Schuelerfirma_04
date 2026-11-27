using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Products;

/// <summary>Validation rules for products and categories, shared by the request classes and the service.</summary>
public static class ProductRules
{
    /// <summary>Whether <paramref name="price"/> is between 0 and the column maximum with at most two decimal places.</summary>
    public static bool IsValidPrice(decimal price) =>
        price >= 0
        && price <= Product.PriceMaxValue
        && decimal.Round(price, Product.PriceDecimalPlaces) == price;

    /// <summary>Removes leading and trailing white space; an empty or blank text becomes <c>null</c>.</summary>
    public static string? NormalizeOptional(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
