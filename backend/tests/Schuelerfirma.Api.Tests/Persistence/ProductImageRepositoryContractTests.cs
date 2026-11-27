using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Tests.Persistence;

/// <summary>Contract of <c>IProductImageRepository</c> and of <c>IImageRepository</c>, which reads the files.</summary>
public abstract class ProductImageRepositoryContractTests : RepositoryContractTests
{
    private async Task<(Repositories Repos, Category Category, Product Product)> ProductAsync(bool isActive = true, bool categoryVisible = true)
    {
        var repos = CreateRepositories();
        var category = await AddCategoryAsync(repos, isVisible: categoryVisible);
        var product = await AddProductAsync(repos, category.CategoryId, isActive: isActive);
        return (repos, category, product);
    }

    [Fact]
    public async Task Add_StoresImageDataAndAssignment_AndGetReturnsThemTogether()
    {
        var (repos, _, product) = await ProductAsync();

        var added = await AddImageAsync(repos, product.ProductId, sortOrder: 3, isPrimary: true);
        var loaded = await repos.ProductImages.GetAsync(product.ProductId, added.ImageId);
        var file = await repos.Images.GetFileAsync(added.ImageId);

        Assert.True(added.ImageId > 0);
        Assert.Equal(added.ImageId, added.Image!.ImageId);
        Assert.Equal((3, true), (loaded!.SortOrder, loaded.IsPrimary));
        Assert.Equal(("bild-3.jpg", "Bild 3", "image/jpeg", (long)ImageData.Length),
            (loaded.Image!.FileName, loaded.Image.Description, loaded.Image.ContentType, loaded.Image.Length));
        Assert.Equal("image/jpeg", file!.ContentType);
        Assert.Equal(ImageData, file.Data);
    }

    [Fact]
    public async Task Add_ToUnknownProduct_Throws_AndStoresNothing()
    {
        var repos = CreateRepositories();
        var image = NewImage(1, ImageData.Length);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repos.ProductImages.AddAsync(
            new ProductImage { ProductId = 9999, Image = image, SortOrder = 0 }, image, ImageData));

        // The transaction keeps image and assignment together: no image without owner is left behind.
        Assert.Null(await repos.Images.GetFileAsync(1));
    }

    [Fact]
    public async Task AnImage_IsOnlyFound_TogetherWithItsOwnProduct()
    {
        var (repos, category, product) = await ProductAsync();
        var other = await AddProductAsync(repos, category.CategoryId, "Orangensaft");
        var image = await AddImageAsync(repos, product.ProductId, sortOrder: 0);

        Assert.Null(await repos.ProductImages.GetAsync(other.ProductId, image.ImageId));
        Assert.False(await repos.ProductImages.ContainsAsync(other.ProductId, image.ImageId, visibleOnly: false));
        Assert.False(await repos.ProductImages.DeleteAsync(other.ProductId, image.ImageId));
        Assert.False(await repos.ProductImages.UpdateAsync(new ProductImage
        {
            ProductId = other.ProductId, ImageId = image.ImageId, Image = new Image { Description = "X" },
        }));
        Assert.True(await repos.ProductImages.ContainsAsync(product.ProductId, image.ImageId, visibleOnly: false));
        Assert.Equal("Bild 0", (await repos.ProductImages.GetAsync(product.ProductId, image.ImageId))!.Image!.Description);
    }

    [Fact]
    public async Task AddingAMainImage_TakesTheFlagFromTheOtherImagesOfTheProduct()
    {
        var (repos, category, product) = await ProductAsync();
        var other = await AddProductAsync(repos, category.CategoryId, "Orangensaft");
        var first = await AddImageAsync(repos, product.ProductId, sortOrder: 0, isPrimary: true);
        var otherMain = await AddImageAsync(repos, other.ProductId, sortOrder: 0, isPrimary: true);

        var second = await AddImageAsync(repos, product.ProductId, sortOrder: 1, isPrimary: true);

        Assert.False((await repos.ProductImages.GetAsync(product.ProductId, first.ImageId))!.IsPrimary);
        Assert.True((await repos.ProductImages.GetAsync(product.ProductId, second.ImageId))!.IsPrimary);
        Assert.True((await repos.ProductImages.GetAsync(other.ProductId, otherMain.ImageId))!.IsPrimary);
    }

    [Fact]
    public async Task Update_ChangesDescriptionOrderAndMainImage_AndLeavesAtMostOneMainImage()
    {
        var (repos, _, product) = await ProductAsync();
        var first = await AddImageAsync(repos, product.ProductId, sortOrder: 0, isPrimary: true);
        var second = await AddImageAsync(repos, product.ProductId, sortOrder: 1);

        var updated = await repos.ProductImages.UpdateAsync(new ProductImage
        {
            ProductId = product.ProductId,
            ImageId = second.ImageId,
            Image = new Image { Description = null },
            SortOrder = 7,
            IsPrimary = true,
        });

        var loaded = await repos.Products.GetByIdAsync(product.ProductId);
        Assert.True(updated);
        Assert.Equal([first.ImageId, second.ImageId], loaded!.Images.Select(i => i.ImageId));
        Assert.Equal([false, true], loaded.Images.Select(i => i.IsPrimary));
        Assert.Null(loaded.Images.Last().Image!.Description);
        Assert.Equal(7, loaded.Images.Last().SortOrder);
        // File name and data stay as they are.
        Assert.Equal("bild-1.jpg", loaded.Images.Last().Image!.FileName);
        Assert.Equal(ImageData, (await repos.Images.GetFileAsync(second.ImageId))!.Data);
    }

    [Fact]
    public async Task Update_OfAnUnknownImage_KeepsTheCurrentMainImage()
    {
        var (repos, _, product) = await ProductAsync();
        var main = await AddImageAsync(repos, product.ProductId, sortOrder: 0, isPrimary: true);

        var updated = await repos.ProductImages.UpdateAsync(new ProductImage
        {
            ProductId = product.ProductId, ImageId = 9999, Image = new Image(), IsPrimary = true,
        });

        Assert.False(updated);
        Assert.True((await repos.ProductImages.GetAsync(product.ProductId, main.ImageId))!.IsPrimary);
    }

    [Fact]
    public async Task Delete_RemovesImageDataAndAssignment_AndUnknownReturnsFalse()
    {
        var (repos, _, product) = await ProductAsync();
        var image = await AddImageAsync(repos, product.ProductId, sortOrder: 0);
        var kept = await AddImageAsync(repos, product.ProductId, sortOrder: 1);

        Assert.True(await repos.ProductImages.DeleteAsync(product.ProductId, image.ImageId));

        Assert.Null(await repos.ProductImages.GetAsync(product.ProductId, image.ImageId));
        Assert.Null(await repos.Images.GetFileAsync(image.ImageId));
        Assert.False(await repos.ProductImages.DeleteAsync(product.ProductId, image.ImageId));
        Assert.Equal([kept.ImageId], (await repos.Products.GetByIdAsync(product.ProductId))!.Images.Select(i => i.ImageId));
    }

    [Fact]
    public async Task Contains_WithVisibleOnly_IsTrueForAVisibleProduct()
    {
        var (repos, _, product) = await ProductAsync();
        var image = await AddImageAsync(repos, product.ProductId, sortOrder: 0);

        Assert.True(await repos.ProductImages.ContainsAsync(product.ProductId, image.ImageId, visibleOnly: true));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Contains_WithVisibleOnly_IsFalseForAnInactiveProduct_OrInAHiddenCategory(bool isActive, bool categoryVisible)
    {
        var (repos, _, product) = await ProductAsync(isActive, categoryVisible);
        var image = await AddImageAsync(repos, product.ProductId, sortOrder: 0);

        Assert.False(await repos.ProductImages.ContainsAsync(product.ProductId, image.ImageId, visibleOnly: true));
        Assert.True(await repos.ProductImages.ContainsAsync(product.ProductId, image.ImageId, visibleOnly: false));
    }

    [Fact]
    public async Task UnknownIds_GiveNullOrFalse()
    {
        var (repos, _, product) = await ProductAsync();

        Assert.Null(await repos.ProductImages.GetAsync(product.ProductId, 9999));
        Assert.False(await repos.ProductImages.ContainsAsync(9999, 9999, visibleOnly: false));
        Assert.Null(await repos.Images.GetFileAsync(9999));
    }
}
