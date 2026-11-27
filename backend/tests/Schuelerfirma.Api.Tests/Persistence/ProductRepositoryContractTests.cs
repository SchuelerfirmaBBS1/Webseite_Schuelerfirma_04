using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Tests.Persistence;

public abstract class ProductRepositoryContractTests : RepositoryContractTests
{
    [Fact]
    public async Task Add_AssignsId_AndGetByIdLoadsCategoryAndAllFields()
    {
        var repos = CreateRepositories();
        var category = await AddCategoryAsync(repos, "Getränke");

        var product = await AddProductAsync(repos, category.CategoryId, "Apfelsaft", price: 12.34m, isPriceVisible: false);
        var loaded = await repos.Products.GetByIdAsync(product.ProductId);

        Assert.True(product.ProductId > 0);
        Assert.NotNull(loaded);
        Assert.Equal("Apfelsaft", loaded.Name);
        Assert.Equal("Naturtrüb", loaded.Description);
        Assert.Equal(12.34m, loaded.Price);
        Assert.False(loaded.IsPriceVisible);
        Assert.True(loaded.IsActive);
        Assert.Equal("Getränke", loaded.Category?.Name);
        Assert.Empty(loaded.Images);
    }

    [Fact]
    public async Task GetById_Unknown_ReturnsNull()
    {
        Assert.Null(await CreateRepositories().Products.GetByIdAsync(9999));
    }

    [Fact]
    public async Task Add_WithUnknownCategory_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => AddProductAsync(CreateRepositories(), categoryId: 9999));
    }

    [Fact]
    public async Task GetAll_ContainsHiddenProducts_OrderedByName_AndFiltersByCategory()
    {
        var repos = CreateRepositories();
        var drinks = await AddCategoryAsync(repos, "Getränke");
        var hidden = await AddCategoryAsync(repos, "Geheim", isVisible: false);
        await AddProductAsync(repos, drinks.CategoryId, "wasser");
        await AddProductAsync(repos, drinks.CategoryId, "Apfelsaft", isActive: false);
        await AddProductAsync(repos, hidden.CategoryId, "Kekse");

        var all = await repos.Products.GetAllAsync();
        var onlyDrinks = await repos.Products.GetAllAsync(drinks.CategoryId);

        Assert.Equal(["Apfelsaft", "Kekse", "wasser"], all.Select(p => p.Name));
        Assert.Equal(["Apfelsaft", "wasser"], onlyDrinks.Select(p => p.Name));
    }

    [Fact]
    public async Task GetVisibleProducts_ContainsOnlyActiveProductsInVisibleCategories()
    {
        var repos = CreateRepositories();
        var drinks = await AddCategoryAsync(repos, "Getränke");
        var hidden = await AddCategoryAsync(repos, "Geheim", isVisible: false);
        await AddProductAsync(repos, drinks.CategoryId, "Wasser");
        await AddProductAsync(repos, drinks.CategoryId, "Apfelsaft");
        await AddProductAsync(repos, drinks.CategoryId, "Alter Saft", isActive: false);
        await AddProductAsync(repos, hidden.CategoryId, "Kekse");

        var visible = await repos.Products.GetVisibleProductsAsync();

        Assert.Equal(["Apfelsaft", "Wasser"], visible.Select(p => p.Name));
        Assert.All(visible, p => Assert.Equal("Getränke", p.Category?.Name));
    }

    [Fact]
    public async Task GetVisibleProducts_FiltersByCategory_AndAHiddenCategoryGivesNothing()
    {
        var repos = CreateRepositories();
        var drinks = await AddCategoryAsync(repos, "Getränke");
        var snacks = await AddCategoryAsync(repos, "Snacks");
        var hidden = await AddCategoryAsync(repos, "Geheim", isVisible: false);
        await AddProductAsync(repos, drinks.CategoryId, "Wasser");
        await AddProductAsync(repos, snacks.CategoryId, "Kekse");
        await AddProductAsync(repos, hidden.CategoryId, "Geheimrezept");

        Assert.Equal(["Kekse"], (await repos.Products.GetVisibleProductsAsync(snacks.CategoryId)).Select(p => p.Name));
        Assert.Empty(await repos.Products.GetVisibleProductsAsync(hidden.CategoryId));
        Assert.Empty(await repos.Products.GetVisibleProductsAsync(9999));
    }

    [Fact]
    public async Task GetVisibleProductById_ReturnsNull_ForInactive_HiddenCategory_AndUnknown()
    {
        var repos = CreateRepositories();
        var drinks = await AddCategoryAsync(repos, "Getränke");
        var hidden = await AddCategoryAsync(repos, "Geheim", isVisible: false);
        var visible = await AddProductAsync(repos, drinks.CategoryId, "Wasser");
        var inactive = await AddProductAsync(repos, drinks.CategoryId, "Alter Saft", isActive: false);
        var inHidden = await AddProductAsync(repos, hidden.CategoryId, "Kekse");

        Assert.Equal("Wasser", (await repos.Products.GetVisibleProductByIdAsync(visible.ProductId))?.Name);
        Assert.Null(await repos.Products.GetVisibleProductByIdAsync(inactive.ProductId));
        Assert.Null(await repos.Products.GetVisibleProductByIdAsync(inHidden.ProductId));
        Assert.Null(await repos.Products.GetVisibleProductByIdAsync(9999));
    }

    [Fact]
    public async Task HidingTheCategoryLater_HidesItsProducts()
    {
        var repos = CreateRepositories();
        var drinks = await AddCategoryAsync(repos, "Getränke");
        var product = await AddProductAsync(repos, drinks.CategoryId, "Wasser");

        await repos.Categories.UpdateAsync(new Category { CategoryId = drinks.CategoryId, Name = drinks.Name, IsVisible = false });

        Assert.Empty(await repos.Products.GetVisibleProductsAsync());
        Assert.Null(await repos.Products.GetVisibleProductByIdAsync(product.ProductId));
        Assert.NotNull(await repos.Products.GetByIdAsync(product.ProductId));
    }

    [Fact]
    public async Task HiddenPrice_IsStillStored_TheRepositoryDoesNotHideIt()
    {
        var repos = CreateRepositories();
        var drinks = await AddCategoryAsync(repos);
        var product = await AddProductAsync(repos, drinks.CategoryId, price: 2.50m, isPriceVisible: false);

        var visible = await repos.Products.GetVisibleProductByIdAsync(product.ProductId);

        Assert.Equal(2.50m, visible?.Price);
        Assert.False(visible!.IsPriceVisible);
    }

    [Fact]
    public async Task Images_AreLoaded_OrderedBySortOrder()
    {
        var repos = CreateRepositories();
        var drinks = await AddCategoryAsync(repos);
        var product = await AddProductAsync(repos, drinks.CategoryId);
        await AddImageAsync(repos, product.ProductId, sortOrder: 2);
        await AddImageAsync(repos, product.ProductId, sortOrder: 0, isPrimary: true);
        await AddImageAsync(repos, product.ProductId, sortOrder: 1);

        var loaded = await repos.Products.GetByIdAsync(product.ProductId);
        var visible = await repos.Products.GetVisibleProductsAsync();

        Assert.Equal([0, 1, 2], loaded!.Images.Select(i => i.SortOrder));
        Assert.True(loaded.Images.First().IsPrimary);
        Assert.Equal([0, 1, 2], visible.Single().Images.Select(i => i.SortOrder));
    }

    [Fact]
    public async Task Update_ChangesAllFields_ButKeepsImages()
    {
        var repos = CreateRepositories();
        var drinks = await AddCategoryAsync(repos, "Getränke");
        var snacks = await AddCategoryAsync(repos, "Snacks");
        var product = await AddProductAsync(repos, drinks.CategoryId, "Apfelsaft", price: 1.50m);
        await AddImageAsync(repos, product.ProductId, sortOrder: 0);

        await repos.Products.UpdateAsync(new Product
        {
            ProductId = product.ProductId,
            CategoryId = snacks.CategoryId,
            Name = "Apfelchips",
            Description = null,
            Price = null,
            IsPriceVisible = false,
            IsActive = false,
        });
        var loaded = await repos.Products.GetByIdAsync(product.ProductId);

        Assert.Equal("Apfelchips", loaded?.Name);
        Assert.Equal("Snacks", loaded!.Category?.Name);
        Assert.Null(loaded.Description);
        Assert.Null(loaded.Price);
        Assert.False(loaded.IsPriceVisible);
        Assert.False(loaded.IsActive);
        Assert.Single(loaded.Images);
    }

    [Fact]
    public async Task Update_Unknown_Throws_AndUnknownCategory_Throws()
    {
        var repos = CreateRepositories();
        var drinks = await AddCategoryAsync(repos);
        var product = await AddProductAsync(repos, drinks.CategoryId);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            repos.Products.UpdateAsync(new Product { ProductId = 9999, CategoryId = drinks.CategoryId, Name = "X" }));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repos.Products.UpdateAsync(new Product { ProductId = product.ProductId, CategoryId = 9999, Name = "X" }));
    }

    [Fact]
    public async Task ReturnedProducts_AreCopies_ChangesNeedUpdate()
    {
        var repos = CreateRepositories();
        var drinks = await AddCategoryAsync(repos);
        var product = await AddProductAsync(repos, drinks.CategoryId, "Apfelsaft");

        var first = await repos.Products.GetByIdAsync(product.ProductId);
        first!.Name = "Geändert";
        first.Category!.Name = "Geändert";

        var second = await repos.Products.GetByIdAsync(product.ProductId);
        Assert.Equal("Apfelsaft", second?.Name);
        Assert.Equal("Getränke", second!.Category?.Name);
    }

    [Fact]
    public async Task SetPriceVisibility_ChangesOnlyThatFlag()
    {
        var repos = CreateRepositories();
        var drinks = await AddCategoryAsync(repos);
        var product = await AddProductAsync(repos, drinks.CategoryId, "Apfelsaft", price: 1.50m);

        Assert.True(await repos.Products.SetPriceVisibilityAsync(product.ProductId, false));
        var loaded = await repos.Products.GetByIdAsync(product.ProductId);

        Assert.False(loaded!.IsPriceVisible);
        Assert.Equal(1.50m, loaded.Price);
        Assert.Equal("Apfelsaft", loaded.Name);
        Assert.True(loaded.IsActive);
        Assert.False(await repos.Products.SetPriceVisibilityAsync(9999, true));
    }

    [Fact]
    public async Task Delete_RemovesProductAndItsImages_AndUnknownReturnsFalse()
    {
        var repos = CreateRepositories();
        var drinks = await AddCategoryAsync(repos);
        var product = await AddProductAsync(repos, drinks.CategoryId);
        var image = await AddImageAsync(repos, product.ProductId, sortOrder: 0);

        Assert.True(await repos.Products.DeleteAsync(product.ProductId));
        Assert.Null(await repos.Images.GetFileAsync(ImageOwner.Product(product.ProductId), image.ImageId));
        Assert.Null(await repos.Products.GetByIdAsync(product.ProductId));
        Assert.False(await repos.Products.DeleteAsync(product.ProductId));

        // Nothing of the product is left behind, so its category is empty and can go.
        Assert.Equal(CategoryDeleteResult.Deleted, await repos.Categories.DeleteAsync(drinks.CategoryId));
    }

    [Fact]
    public async Task ImageData_LargerThanABlob_IsStoredUnchanged_AndLengthIsLoaded()
    {
        var repos = CreateRepositories();
        var drinks = await AddCategoryAsync(repos);
        var product = await AddProductAsync(repos, drinks.CategoryId);
        // 1 MiB: a plain BLOB column (64 KiB) would cut it off, MEDIUMBLOB holds it.
        var data = new byte[1024 * 1024];
        Random.Shared.NextBytes(data);
        var image = NewImage(product.ProductId, sortOrder: 0);
        image.Length = data.Length;

        await repos.Images.AddAsync(image, data);

        Assert.Equal(data, (await repos.Images.GetFileAsync(ImageOwner.Product(product.ProductId), image.ImageId))?.Data);
        Assert.Equal(data.Length, (await repos.Products.GetByIdAsync(product.ProductId))!.Images.Single().Length);
    }
}
