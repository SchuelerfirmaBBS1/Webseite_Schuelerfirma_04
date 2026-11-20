using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.InMemory;

/// <summary>
/// The shared data of the in-memory repositories. Everything is guarded by <see cref="Sync"/>, and
/// only copies leave the store, which mimics detached EF Core entities. For development and tests only.
/// </summary>
public sealed class InMemoryStore
{
    /// <summary>Lock that every repository holds while it reads or writes the store.</summary>
    internal object Sync { get; } = new();

    internal List<Role> Roles { get; } = [.. DefaultRoles.CreateSeed()];

    internal List<User> Users { get; } = [];

    internal Dictionary<string, UserSession> Sessions { get; } = [];

    internal int LastUserId { get; set; }

    internal List<Category> Categories { get; } = [];

    internal List<Product> Products { get; } = [];

    internal List<ProductImage> ProductImages { get; } = [];

    internal int LastCategoryId { get; set; }

    internal int LastProductId { get; set; }

    internal static bool SameText(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    internal static Role Copy(Role role) => new() { RoleId = role.RoleId, Name = role.Name };

    internal static User Copy(User user, Role? role = null) => new()
    {
        UserId = user.UserId,
        RoleId = user.RoleId,
        Role = role is null ? null : Copy(role),
        Username = user.Username,
        Email = user.Email,
        PasswordHash = user.PasswordHash,
        IsActive = user.IsActive,
        FailedLoginCount = user.FailedLoginCount,
        LockoutEnd = user.LockoutEnd,
        MustChangePassword = user.MustChangePassword,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt,
    };

    internal static UserSession Copy(UserSession session) => new()
    {
        SessionKeyHash = session.SessionKeyHash,
        UserId = session.UserId,
        Ticket = (byte[])session.Ticket.Clone(),
        CreatedAt = session.CreatedAt,
        ExpiresAt = session.ExpiresAt,
    };

    internal static Category Copy(Category category) => new()
    {
        CategoryId = category.CategoryId,
        Name = category.Name,
        IsVisible = category.IsVisible,
    };

    internal static ProductImage Copy(ProductImage image) => new()
    {
        ProductImageId = image.ProductImageId,
        ProductId = image.ProductId,
        FileName = image.FileName,
        Description = image.Description,
        ContentType = image.ContentType,
        FilePath = image.FilePath,
        SortOrder = image.SortOrder,
        IsPrimary = image.IsPrimary,
    };

    /// <summary>Copy of a stored user with its role loaded.</summary>
    internal User Materialize(User stored) => Copy(stored, Roles.Single(r => r.RoleId == stored.RoleId));

    /// <summary>Copy of a stored product with its category and its images (ordered) loaded.</summary>
    internal Product Materialize(Product stored) => new()
    {
        ProductId = stored.ProductId,
        CategoryId = stored.CategoryId,
        Category = Copy(Categories.Single(c => c.CategoryId == stored.CategoryId)),
        Name = stored.Name,
        Description = stored.Description,
        Price = stored.Price,
        IsPriceVisible = stored.IsPriceVisible,
        IsActive = stored.IsActive,
        Images =
        [
            .. ProductImages
                .Where(i => i.ProductId == stored.ProductId)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.ProductImageId)
                .Select(Copy),
        ],
    };
}
