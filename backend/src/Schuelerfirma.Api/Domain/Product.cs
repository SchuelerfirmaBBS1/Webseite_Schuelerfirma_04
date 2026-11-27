namespace Schuelerfirma.Api.Domain;

/// <summary>
/// A product of the student company. Visitors see it only if it is active and its category is visible; the price
/// only if <see cref="IsPriceVisible"/> is set.
/// </summary>
public class Product
{
    /// <summary>Maximum length of <see cref="Name"/>.</summary>
    public const int NameMaxLength = 200;

    /// <summary>Maximum length of <see cref="Description"/>.</summary>
    public const int DescriptionMaxLength = 4000;

    /// <summary>Number of decimal places a price may have.</summary>
    public const int PriceDecimalPlaces = 2;

    /// <summary>Highest price the database column (<c>decimal(10,2)</c>) can hold.</summary>
    /// <remarks>
    /// A <c>decimal</c> on purpose: as a <c>double</c> the value is not exact, and depending on the runtime the
    /// conversion to <c>decimal</c> ends up slightly below 99,999,999.99, which then rejects the highest price.
    /// </remarks>
    public const decimal PriceMaxValue = 99_999_999.99m;

    /// <summary>Primary key.</summary>
    public int ProductId { get; set; }

    /// <summary>The category the product belongs to.</summary>
    public int CategoryId { get; set; }

    /// <summary>The category; loaded by the repositories.</summary>
    public Category? Category { get; set; }

    /// <summary>Name; plain text, never HTML.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description; plain text, never HTML.</summary>
    public string? Description { get; set; }

    /// <summary>Price in euros (at least 0, at most two decimal places), or <c>null</c> if there is none.</summary>
    public decimal? Price { get; set; }

    /// <summary>Whether visitors see the price. If not, the public API answers with <c>price: null</c>.</summary>
    public bool IsPriceVisible { get; set; }

    /// <summary>Whether visitors see the product at all.</summary>
    public bool IsActive { get; set; }

    /// <summary>Images of the product, ordered by <see cref="ProductImage.SortOrder"/> when loaded by a repository.</summary>
    public ICollection<ProductImage> Images { get; set; } = [];
}
