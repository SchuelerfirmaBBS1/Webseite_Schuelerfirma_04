using Microsoft.Extensions.Logging;
using Schuelerfirma.Api.Application.Products;
using Schuelerfirma.Api.Domain;
using Schuelerfirma.Api.Infrastructure.InMemory;
using Schuelerfirma.Api.Tests.Auth;

namespace Schuelerfirma.Api.Tests.Products;

public class ProductCatalogServiceTests
{
    private const int Actor = 7;

    private readonly InMemoryStore store = new();
    private readonly InMemoryProductRepository products;
    private readonly ListLoggerProvider logs = new();
    private readonly ProductCatalogService service;

    public ProductCatalogServiceTests()
    {
        products = new InMemoryProductRepository(store);
        var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(logs));
        service = new ProductCatalogService(products, new InMemoryCategoryRepository(store), loggerFactory.CreateLogger<ProductCatalogService>());
    }

    private async Task<Category> CategoryAsync(string name = "Getränke", bool isVisible = true) =>
        (await service.CreateCategoryAsync(Actor, name, isVisible)).Value!;

    private static ProductInput Input(int categoryId, string name = "Apfelsaft", decimal? price = 1.50m, string? description = "Naturtrüb") =>
        new(categoryId, name, description, price, IsPriceVisible: true, IsActive: true);

    // ---- products ----

    [Fact]
    public async Task CreateProduct_TrimsTheName_StoresBlankDescriptionAsNull_AndLogsTheActor()
    {
        var category = await CategoryAsync();

        var result = await service.CreateProductAsync(Actor, Input(category.CategoryId, "  Apfelsaft  ", description: "   "));

        Assert.Equal(CatalogError.None, result.Error);
        Assert.Equal("Apfelsaft", result.Value!.Name);
        Assert.Null(result.Value.Description);
        Assert.Equal("Getränke", result.Value.Category?.Name);
        Assert.Contains($"User {Actor} created product Apfelsaft (id {result.Value.ProductId})", logs.AllText);
    }

    [Fact]
    public async Task CreateProduct_WithUnknownCategory_IsInvalidAtTheCategoryField()
    {
        var result = await service.CreateProductAsync(Actor, Input(categoryId: 9999));

        Assert.Equal(CatalogError.Invalid, result.Error);
        Assert.Equal(nameof(ProductInput.CategoryId), result.Field);
        Assert.Empty(await products.GetAllAsync());
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("1.234")]
    [InlineData("100000000")]
    public async Task CreateProduct_WithInvalidPrice_IsInvalid(string price)
    {
        var category = await CategoryAsync();

        var result = await service.CreateProductAsync(
            Actor, Input(category.CategoryId, price: decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal(CatalogError.Invalid, result.Error);
        Assert.Equal(nameof(ProductInput.Price), result.Field);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0.5")]
    [InlineData("12.34")]
    [InlineData("99999999.99")]
    public async Task CreateProduct_WithValidPrice_Succeeds(string price)
    {
        var category = await CategoryAsync();
        var value = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture);

        var result = await service.CreateProductAsync(Actor, Input(category.CategoryId, price: value));

        Assert.Equal(CatalogError.None, result.Error);
        Assert.Equal(value, result.Value!.Price);
    }

    [Fact]
    public async Task CreateProduct_WithoutPrice_Succeeds()
    {
        var category = await CategoryAsync();

        var result = await service.CreateProductAsync(Actor, Input(category.CategoryId, price: null));

        Assert.Equal(CatalogError.None, result.Error);
        Assert.Null(result.Value!.Price);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateProduct_WithBlankName_IsInvalid(string name)
    {
        var category = await CategoryAsync();

        var result = await service.CreateProductAsync(Actor, Input(category.CategoryId, name));

        Assert.Equal(CatalogError.Invalid, result.Error);
        Assert.Equal(nameof(ProductInput.Name), result.Field);
    }

    [Fact]
    public async Task CreateProduct_WithTooLongTexts_IsInvalid()
    {
        var category = await CategoryAsync();

        var longName = await service.CreateProductAsync(Actor, Input(category.CategoryId, new string('a', Product.NameMaxLength + 1)));
        var longDescription = await service.CreateProductAsync(
            Actor, Input(category.CategoryId, description: new string('a', Product.DescriptionMaxLength + 1)));

        Assert.Equal(nameof(ProductInput.Name), longName.Field);
        Assert.Equal(nameof(ProductInput.Description), longDescription.Field);
    }

    [Fact]
    public async Task UpdateProduct_ChangesTheFields_AndUnknownIsNotFound()
    {
        var drinks = await CategoryAsync();
        var snacks = await CategoryAsync("Snacks");
        var product = (await service.CreateProductAsync(Actor, Input(drinks.CategoryId))).Value!;

        var result = await service.UpdateProductAsync(
            Actor, product.ProductId, new ProductInput(snacks.CategoryId, "Apfelchips", null, 2.00m, false, false));
        var unknown = await service.UpdateProductAsync(Actor, 9999, Input(drinks.CategoryId));

        Assert.Equal(CatalogError.None, result.Error);
        Assert.Equal("Apfelchips", result.Value!.Name);
        Assert.Equal("Snacks", result.Value.Category?.Name);
        Assert.False(result.Value.IsActive);
        Assert.Equal(CatalogError.NotFound, unknown.Error);
    }

    [Fact]
    public async Task UpdateProduct_ToUnknownCategory_IsInvalid()
    {
        var drinks = await CategoryAsync();
        var product = (await service.CreateProductAsync(Actor, Input(drinks.CategoryId))).Value!;

        var result = await service.UpdateProductAsync(Actor, product.ProductId, Input(categoryId: 9999));

        Assert.Equal(CatalogError.Invalid, result.Error);
        Assert.Equal(drinks.CategoryId, (await products.GetByIdAsync(product.ProductId))!.CategoryId);
    }

    [Fact]
    public async Task SetPriceVisibility_ChangesTheFlag_AndUnknownIsNotFound()
    {
        var drinks = await CategoryAsync();
        var product = (await service.CreateProductAsync(Actor, Input(drinks.CategoryId))).Value!;

        var result = await service.SetPriceVisibilityAsync(Actor, product.ProductId, false);

        Assert.Equal(CatalogError.None, result.Error);
        Assert.False((await products.GetByIdAsync(product.ProductId))!.IsPriceVisible);
        Assert.Contains($"User {Actor} hid the price of product {product.ProductId}", logs.AllText);
        Assert.Equal(CatalogError.NotFound, (await service.SetPriceVisibilityAsync(Actor, 9999, true)).Error);
    }

    [Fact]
    public async Task DeleteProduct_ReturnsTheDeletedProduct_AndUnknownIsNotFound()
    {
        var drinks = await CategoryAsync();
        var product = (await service.CreateProductAsync(Actor, Input(drinks.CategoryId))).Value!;

        var result = await service.DeleteProductAsync(Actor, product.ProductId);

        Assert.Equal(CatalogError.None, result.Error);
        Assert.Equal("Apfelsaft", result.Value?.Name);
        Assert.Null(await products.GetByIdAsync(product.ProductId));
        Assert.Equal(CatalogError.NotFound, (await service.DeleteProductAsync(Actor, product.ProductId)).Error);
    }

    // ---- categories ----

    [Fact]
    public async Task CreateCategory_WithDuplicateName_IgnoringCase_IsAConflict()
    {
        await CategoryAsync("Getränke");

        var result = await service.CreateCategoryAsync(Actor, " getränke ", true);

        Assert.Equal(CatalogError.Conflict, result.Error);
        Assert.Equal(nameof(Category.Name), result.Field);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task CreateCategory_WithBlankName_IsInvalid(string name)
    {
        var result = await service.CreateCategoryAsync(Actor, name, true);

        Assert.Equal(CatalogError.Invalid, result.Error);
    }

    [Fact]
    public async Task UpdateCategory_RenamesAndHides_ConflictsWithAnotherName_AndUnknownIsNotFound()
    {
        var drinks = await CategoryAsync("Getränke");
        await CategoryAsync("Snacks");

        var renamed = await service.UpdateCategoryAsync(Actor, drinks.CategoryId, "Kaltgetränke", false);
        var duplicate = await service.UpdateCategoryAsync(Actor, drinks.CategoryId, "SNACKS", true);
        var unknown = await service.UpdateCategoryAsync(Actor, 9999, "X", true);

        Assert.Equal(CatalogError.None, renamed.Error);
        Assert.False(renamed.Value!.IsVisible);
        Assert.Equal(CatalogError.Conflict, duplicate.Error);
        Assert.Equal(CatalogError.NotFound, unknown.Error);
    }

    [Fact]
    public async Task DeleteCategory_WithProducts_IsAConflict_EmptyOneIsDeleted()
    {
        var drinks = await CategoryAsync("Getränke");
        var empty = await CategoryAsync("Leer");
        await service.CreateProductAsync(Actor, Input(drinks.CategoryId));

        var withProducts = await service.DeleteCategoryAsync(Actor, drinks.CategoryId);
        var deleted = await service.DeleteCategoryAsync(Actor, empty.CategoryId);
        var unknown = await service.DeleteCategoryAsync(Actor, empty.CategoryId);

        Assert.Equal(CatalogError.Conflict, withProducts.Error);
        Assert.Contains("Produkte", withProducts.Message);
        Assert.Equal(CatalogError.None, deleted.Error);
        Assert.Equal(CatalogError.NotFound, unknown.Error);
    }
}
