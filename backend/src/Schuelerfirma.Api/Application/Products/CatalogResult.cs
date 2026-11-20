namespace Schuelerfirma.Api.Application.Products;

/// <summary>Why an action of <see cref="ProductCatalogService"/> did not succeed.</summary>
public enum CatalogError
{
    /// <summary>The action succeeded.</summary>
    None,

    /// <summary>The product or category does not exist.</summary>
    NotFound,

    /// <summary>An input is invalid; see <see cref="CatalogResult{T}.Field"/> and the message.</summary>
    Invalid,

    /// <summary>The action breaks a rule (duplicate name, category still has products); see the message.</summary>
    Conflict,
}

/// <summary>Result of an action of <see cref="ProductCatalogService"/>.</summary>
/// <typeparam name="T">Type of the changed entity.</typeparam>
/// <param name="Error">Whether and why the action failed.</param>
/// <param name="Value">The entity after the action (for a delete: as it was before), if available.</param>
/// <param name="Field">The input the message refers to (property name of the request), if any.</param>
/// <param name="Message">German message that can be shown in the admin panel.</param>
public sealed record CatalogResult<T>(
    CatalogError Error,
    T? Value = null,
    string? Field = null,
    string? Message = null)
    where T : class
{
    /// <summary>The action succeeded.</summary>
    public static CatalogResult<T> Success(T? value = null) => new(CatalogError.None, value);

    /// <summary>The entity does not exist.</summary>
    public static CatalogResult<T> NotFound { get; } = new(CatalogError.NotFound);

    /// <summary>An input is invalid.</summary>
    public static CatalogResult<T> Invalid(string field, string message) =>
        new(CatalogError.Invalid, Field: field, Message: message);

    /// <summary>A rule forbids the action.</summary>
    public static CatalogResult<T> Conflict(string message, string? field = null) =>
        new(CatalogError.Conflict, Field: field, Message: message);
}
