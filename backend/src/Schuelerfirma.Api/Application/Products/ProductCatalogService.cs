using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Auth;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Application.Products;

/// <summary>
/// What the team does with products and categories in the admin area, and what visitors may read. Controllers only
/// translate into HTTP; validation, the rules and the log entries live here.
/// </summary>
/// <remarks>
/// Texts are stored as plain text exactly as entered (apart from trimming); they are never interpreted as HTML, and
/// the frontend shows them as text. The rules for visitors (active product, visible category) are part of the
/// repository queries; hiding the price is done by the public response.
/// </remarks>
public sealed class ProductCatalogService(
    IProductRepository products,
    ICategoryRepository categories,
    ILogger<ProductCatalogService> logger)
{
    private const string DuplicateCategoryName = "Es gibt schon eine Kategorie mit diesem Namen.";
    private const string UnknownCategory = "Diese Kategorie gibt es nicht.";

    // ---- visitors ----

    /// <summary>Products visitors may see, optionally of one category, ordered by name.</summary>
    public Task<IReadOnlyList<Product>> GetVisibleProductsAsync(int? categoryId, CancellationToken cancellationToken = default) =>
        products.GetVisibleProductsAsync(categoryId, cancellationToken);

    /// <summary>The product if visitors may see it, otherwise <c>null</c> (hidden and unknown are the same).</summary>
    public Task<Product?> GetVisibleProductAsync(int productId, CancellationToken cancellationToken = default) =>
        products.GetVisibleProductByIdAsync(productId, cancellationToken);

    /// <summary>Categories visitors may see, ordered by name.</summary>
    public Task<IReadOnlyList<Category>> GetVisibleCategoriesAsync(CancellationToken cancellationToken = default) =>
        categories.GetVisibleAsync(cancellationToken);

    // ---- admin area: read ----

    /// <summary>All products with every field, optionally of one category, ordered by name.</summary>
    public Task<IReadOnlyList<Product>> GetProductsAsync(int? categoryId, CancellationToken cancellationToken = default) =>
        products.GetAllAsync(categoryId, cancellationToken);

    /// <summary>The product with every field, or <c>null</c>.</summary>
    public Task<Product?> GetProductAsync(int productId, CancellationToken cancellationToken = default) =>
        products.GetByIdAsync(productId, cancellationToken);

    /// <summary>All categories, ordered by name.</summary>
    public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        categories.GetAllAsync(cancellationToken);

    /// <summary>The category, or <c>null</c>.</summary>
    public Task<Category?> GetCategoryAsync(int categoryId, CancellationToken cancellationToken = default) =>
        categories.GetByIdAsync(categoryId, cancellationToken);

    // ---- admin area: products ----

    /// <summary>Creates a product in an existing category.</summary>
    /// <param name="actingUserId">The user who does this (for the log).</param>
    /// <param name="input">The fields of the product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<CatalogResult<Product>> CreateProductAsync(
        int actingUserId, ProductInput input, CancellationToken cancellationToken = default)
    {
        if (await ValidateAsync(input, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        var product = Apply(input, new Product());
        try
        {
            await products.AddAsync(product, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // The category was deleted between the check and the insert.
            return CatalogResult<Product>.Invalid(nameof(ProductInput.CategoryId), UnknownCategory);
        }

        logger.LogInformation(
            "User {ActorId} created product {ProductName} (id {ProductId})",
            actingUserId, LogText.Safe(product.Name), product.ProductId);
        return CatalogResult<Product>.Success(await products.GetByIdAsync(product.ProductId, cancellationToken));
    }

    /// <summary>Replaces all editable fields of a product; images are left alone.</summary>
    public async Task<CatalogResult<Product>> UpdateProductAsync(
        int actingUserId, int productId, ProductInput input, CancellationToken cancellationToken = default)
    {
        var product = await products.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return CatalogResult<Product>.NotFound;
        }

        if (await ValidateAsync(input, cancellationToken) is { } invalid)
        {
            return invalid;
        }

        try
        {
            await products.UpdateAsync(Apply(input, product), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return CatalogResult<Product>.NotFound;
        }
        catch (InvalidOperationException)
        {
            return CatalogResult<Product>.Invalid(nameof(ProductInput.CategoryId), UnknownCategory);
        }

        logger.LogInformation("User {ActorId} changed product {ProductId}", actingUserId, productId);
        return await products.GetByIdAsync(productId, cancellationToken) is { } updated
            ? CatalogResult<Product>.Success(updated)
            : CatalogResult<Product>.NotFound;
    }

    /// <summary>
    /// Shows or hides the price of a product. A separate action so that a role can be allowed exactly this.
    /// </summary>
    public async Task<CatalogResult<Product>> SetPriceVisibilityAsync(
        int actingUserId, int productId, bool isPriceVisible, CancellationToken cancellationToken = default)
    {
        if (!await products.SetPriceVisibilityAsync(productId, isPriceVisible, cancellationToken))
        {
            return CatalogResult<Product>.NotFound;
        }

        logger.LogInformation(
            "User {ActorId} {Action} the price of product {ProductId}",
            actingUserId, isPriceVisible ? "showed" : "hid", productId);
        return CatalogResult<Product>.Success();
    }

    /// <summary>Deletes a product together with its images (records and image data).</summary>
    /// <returns>On success the product as it was before, including its images.</returns>
    public async Task<CatalogResult<Product>> DeleteProductAsync(
        int actingUserId, int productId, CancellationToken cancellationToken = default)
    {
        var product = await products.GetByIdAsync(productId, cancellationToken);
        if (product is null || !await products.DeleteAsync(productId, cancellationToken))
        {
            return CatalogResult<Product>.NotFound;
        }

        logger.LogInformation(
            "User {ActorId} deleted product {ProductName} (id {ProductId})",
            actingUserId, LogText.Safe(product.Name), productId);
        return CatalogResult<Product>.Success(product);
    }

    // ---- admin area: categories ----

    /// <summary>Creates a category with a unique name (case-insensitive).</summary>
    public async Task<CatalogResult<Category>> CreateCategoryAsync(
        int actingUserId, string name, bool isVisible, CancellationToken cancellationToken = default)
    {
        if (ValidateCategoryName(name) is { } invalid)
        {
            return invalid;
        }

        var category = new Category { Name = name.Trim(), IsVisible = isVisible };
        try
        {
            await categories.AddAsync(category, cancellationToken);
        }
        catch (DuplicateEntryException)
        {
            return CatalogResult<Category>.Conflict(DuplicateCategoryName, nameof(Category.Name));
        }

        logger.LogInformation(
            "User {ActorId} created category {CategoryName} (id {CategoryId})",
            actingUserId, LogText.Safe(category.Name), category.CategoryId);
        return CatalogResult<Category>.Success(category);
    }

    /// <summary>Renames a category and shows or hides it.</summary>
    public async Task<CatalogResult<Category>> UpdateCategoryAsync(
        int actingUserId, int categoryId, string name, bool isVisible, CancellationToken cancellationToken = default)
    {
        if (await categories.GetByIdAsync(categoryId, cancellationToken) is null)
        {
            return CatalogResult<Category>.NotFound;
        }

        if (ValidateCategoryName(name) is { } invalid)
        {
            return invalid;
        }

        var category = new Category { CategoryId = categoryId, Name = name.Trim(), IsVisible = isVisible };
        try
        {
            await categories.UpdateAsync(category, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return CatalogResult<Category>.NotFound;
        }
        catch (DuplicateEntryException)
        {
            return CatalogResult<Category>.Conflict(DuplicateCategoryName, nameof(Category.Name));
        }

        logger.LogInformation("User {ActorId} changed category {CategoryId}", actingUserId, categoryId);
        return CatalogResult<Category>.Success(category);
    }

    /// <summary>Deletes a category; one that still contains products is kept (conflict).</summary>
    public async Task<CatalogResult<Category>> DeleteCategoryAsync(
        int actingUserId, int categoryId, CancellationToken cancellationToken = default)
    {
        var category = await categories.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
        {
            return CatalogResult<Category>.NotFound;
        }

        switch (await categories.DeleteAsync(categoryId, cancellationToken))
        {
            case CategoryDeleteResult.NotFound:
                return CatalogResult<Category>.NotFound;

            case CategoryDeleteResult.HasProducts:
                return CatalogResult<Category>.Conflict(
                    "Die Kategorie enthält noch Produkte. Bitte die Produkte zuerst löschen oder einer anderen Kategorie zuordnen.");

            default:
                logger.LogInformation(
                    "User {ActorId} deleted category {CategoryName} (id {CategoryId})",
                    actingUserId, LogText.Safe(category.Name), categoryId);
                return CatalogResult<Category>.Success(category);
        }
    }

    private async Task<CatalogResult<Product>?> ValidateAsync(ProductInput input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return CatalogResult<Product>.Invalid(nameof(ProductInput.Name), "Bitte einen Namen angeben.");
        }

        if (input.Name.Trim().Length > Product.NameMaxLength)
        {
            return CatalogResult<Product>.Invalid(
                nameof(ProductInput.Name), $"Der Name darf höchstens {Product.NameMaxLength} Zeichen lang sein.");
        }

        if (ProductRules.NormalizeOptional(input.Description)?.Length > Product.DescriptionMaxLength)
        {
            return CatalogResult<Product>.Invalid(
                nameof(ProductInput.Description),
                $"Die Beschreibung darf höchstens {Product.DescriptionMaxLength} Zeichen lang sein.");
        }

        if (input.Price is { } price && !ProductRules.IsValidPrice(price))
        {
            return CatalogResult<Product>.Invalid(
                nameof(ProductInput.Price), "Der Preis muss mindestens 0 sein und darf höchstens zwei Nachkommastellen haben.");
        }

        if (await categories.GetByIdAsync(input.CategoryId, cancellationToken) is null)
        {
            return CatalogResult<Product>.Invalid(nameof(ProductInput.CategoryId), UnknownCategory);
        }

        return null;
    }

    private static CatalogResult<Category>? ValidateCategoryName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return CatalogResult<Category>.Invalid(nameof(Category.Name), "Bitte einen Namen angeben.");
        }

        return name.Trim().Length > Category.NameMaxLength
            ? CatalogResult<Category>.Invalid(
                nameof(Category.Name), $"Der Name darf höchstens {Category.NameMaxLength} Zeichen lang sein.")
            : null;
    }

    private static Product Apply(ProductInput input, Product product)
    {
        product.CategoryId = input.CategoryId;
        product.Name = input.Name.Trim();
        product.Description = ProductRules.NormalizeOptional(input.Description);
        product.Price = input.Price;
        product.IsPriceVisible = input.IsPriceVisible;
        product.IsActive = input.IsActive;
        return product;
    }
}
