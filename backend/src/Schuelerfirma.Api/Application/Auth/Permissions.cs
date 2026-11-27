namespace Schuelerfirma.Api.Application.Auth;

/// <summary>
/// The catalog of permissions. Endpoints request one with <c>[HasPermission(...)]</c>; which role has which
/// permission is configured under <c>Authorization:RolePermissions</c>, not here.
/// </summary>
/// <remarks>
/// A new feature adds its permissions here (constant and <see cref="All"/>) and in the configuration.
/// </remarks>
public static class Permissions
{
    /// <summary>Stands for every permission, including ones added later. Only valid in the configuration.</summary>
    public const string Wildcard = "*";

    /// <summary>Create events.</summary>
    public const string EventsCreate = "events.create";

    /// <summary>Edit events.</summary>
    public const string EventsUpdate = "events.update";

    /// <summary>Delete events.</summary>
    public const string EventsDelete = "events.delete";

    /// <summary>Upload images.</summary>
    public const string ImagesUpload = "images.upload";

    /// <summary>Edit description and order of images.</summary>
    public const string ImagesUpdate = "images.update";

    /// <summary>Delete images.</summary>
    public const string ImagesDelete = "images.delete";

    /// <summary>See all products in the admin area, including inactive ones and hidden prices.</summary>
    public const string ProductsRead = "products.read";

    /// <summary>Create, edit and delete products, categories and product images.</summary>
    public const string ProductsManage = "products.manage";

    /// <summary>Show or hide the price of a product.</summary>
    public const string ProductsTogglePriceVisibility = "products.toggle-price-visibility";

    /// <summary>Read contact requests.</summary>
    public const string ContactRequestsRead = "contact-requests.read";

    /// <summary>Change the status of contact requests.</summary>
    public const string ContactRequestsUpdate = "contact-requests.update";

    /// <summary>Delete contact requests.</summary>
    public const string ContactRequestsDelete = "contact-requests.delete";

    /// <summary>Change application settings such as the contact e-mail address.</summary>
    public const string SettingsManage = "settings.manage";

    /// <summary>Manage users and their roles.</summary>
    public const string UsersManage = "users.manage";

    /// <summary>All permissions of the catalog, in alphabetical order.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        .. new[]
        {
            EventsCreate, EventsUpdate, EventsDelete,
            ImagesUpload, ImagesUpdate, ImagesDelete,
            ProductsRead, ProductsManage, ProductsTogglePriceVisibility,
            ContactRequestsRead, ContactRequestsUpdate, ContactRequestsDelete,
            SettingsManage, UsersManage,
        }.Order(StringComparer.Ordinal),
    ];

    /// <summary>Whether <paramref name="permission"/> is part of the catalog (exact spelling).</summary>
    public static bool IsKnown(string permission) => All.Contains(permission, StringComparer.Ordinal);
}
