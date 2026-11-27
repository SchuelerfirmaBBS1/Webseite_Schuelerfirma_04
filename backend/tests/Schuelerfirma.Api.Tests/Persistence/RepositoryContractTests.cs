using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Tests.Persistence;

/// <summary>The repositories under test; they must share one underlying store.</summary>
/// <param name="AddProductImage">
/// Stores an image record together with its image data directly. Test seam until the image upload (#13) brings
/// repository methods for it.
/// </param>
/// <param name="GetProductImageData">Reads the stored image data of an image, <c>null</c> if there is none.</param>
public sealed record Repositories(
    IUserRepository Users,
    IRoleRepository Roles,
    IUserSessionRepository Sessions,
    ICategoryRepository Categories,
    IProductRepository Products,
    Func<ProductImage, byte[], Task> AddProductImage,
    Func<int, Task<byte[]?>> GetProductImageData);

/// <summary>
/// Base class of all repository contract tests. Every storage implementation (in-memory now, EF Core / MySQL
/// later) derives from the three contract classes and only provides <see cref="CreateRepositories"/>, so all
/// implementations are held to exactly the same behavior.
/// </summary>
public abstract class RepositoryContractTests
{
    /// <summary>Creates repositories on top of a fresh, empty store that contains only the seed roles.</summary>
    protected abstract Repositories CreateRepositories();

    protected static readonly DateTime Now = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    protected static int AdminRoleId => DefaultRoles.CreateSeed().Single(r => r.Name == DefaultRoles.Admin).RoleId;

    protected static int UserRoleId => DefaultRoles.CreateSeed().Single(r => r.Name == DefaultRoles.User).RoleId;

    protected static User NewUser(string username = "anna", string? email = null, int? roleId = null) => new()
    {
        Username = username,
        Email = email ?? $"{username}@example.test",
        PasswordHash = "hash",
        RoleId = roleId ?? UserRoleId,
        IsActive = true,
        CreatedAt = Now,
    };

    protected static async Task<User> AddUserAsync(Repositories repos, string username = "anna", int? roleId = null)
    {
        var user = NewUser(username, roleId: roleId);
        await repos.Users.AddAsync(user);
        return user;
    }

    protected static async Task<Category> AddCategoryAsync(Repositories repos, string name = "Getränke", bool isVisible = true)
    {
        var category = new Category { Name = name, IsVisible = isVisible };
        await repos.Categories.AddAsync(category);
        return category;
    }

    protected static async Task<Product> AddProductAsync(
        Repositories repos,
        int categoryId,
        string name = "Apfelsaft",
        decimal? price = 1.50m,
        bool isPriceVisible = true,
        bool isActive = true)
    {
        var product = new Product
        {
            CategoryId = categoryId,
            Name = name,
            Description = "Naturtrüb",
            Price = price,
            IsPriceVisible = isPriceVisible,
            IsActive = isActive,
        };
        await repos.Products.AddAsync(product);
        return product;
    }

    /// <summary>Stand-in image data (the JPEG signature followed by a few bytes).</summary>
    protected static readonly byte[] ImageData = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4];

    /// <summary>Stores a new image of <paramref name="productId"/> with <see cref="ImageData"/>.</summary>
    protected static async Task<ProductImage> AddImageAsync(
        Repositories repos, int productId, int sortOrder, bool isPrimary = false)
    {
        var image = NewImage(productId, sortOrder, isPrimary);
        await repos.AddProductImage(image, ImageData);
        return image;
    }

    protected static ProductImage NewImage(int productId, int sortOrder, bool isPrimary = false) => new()
    {
        ProductId = productId,
        FileName = $"bild-{sortOrder}.jpg",
        Description = $"Bild {sortOrder}",
        ContentType = "image/jpeg",
        Length = ImageData.Length,
        SortOrder = sortOrder,
        IsPrimary = isPrimary,
    };
}
