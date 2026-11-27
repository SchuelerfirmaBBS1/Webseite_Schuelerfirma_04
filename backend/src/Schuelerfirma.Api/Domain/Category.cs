namespace Schuelerfirma.Api.Domain;

/// <summary>
/// A product category, e.g. "Getränke". Visitors only see categories with <see cref="IsVisible"/> and only the
/// products inside them.
/// </summary>
public class Category
{
    /// <summary>Maximum length of <see cref="Name"/>.</summary>
    public const int NameMaxLength = 100;

    /// <summary>Primary key.</summary>
    public int CategoryId { get; set; }

    /// <summary>Unique name (compared case-insensitively); plain text, never HTML.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether visitors see the category and its products.</summary>
    public bool IsVisible { get; set; }

    /// <summary>Products in this category.</summary>
    public ICollection<Product> Products { get; set; } = [];
}
