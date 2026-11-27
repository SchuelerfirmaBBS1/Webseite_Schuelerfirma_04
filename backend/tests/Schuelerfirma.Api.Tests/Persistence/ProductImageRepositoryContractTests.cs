using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Tests.Persistence;

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
    public async Task Add_AssignsId_StoresRecordAndData_AndGetReturnsTheRecord()
    {
        var (repos, _, product) = await ProductAsync();

        var image = await AddImageAsync(repos, product.ProductId, sortOrder: 3, isPrimary: true);
        var loaded = await repos.ProductImages.GetAsync(product.ProductId, image.ProductImageId);
        var file = await repos.ProductImages.GetFileAsync(product.ProductId, image.ProductImageId);

        Assert.True(image.ProductImageId > 0);
        Assert.Equal(("bild-3.jpg", "Bild 3", "image/jpeg", (long)ImageData.Length, 3, true),
            (loaded!.FileName, loaded.Description, loaded.ContentType, loaded.Length, loaded.SortOrder, loaded.IsPrimary));
        Assert.Equal("image/jpeg", file!.ContentType);
        Assert.Equal(ImageData, file.Data);
    }

    [Fact]
    public async Task Add_ToUnknownProduct_Throws()
    {
        var repos = CreateRepositories();

        await Assert.ThrowsAsync<InvalidOperationException>(() => repos.ProductImages.AddAsync(NewImage(9999, 0), ImageData));
    }

    [Fact]
    public async Task AnImage_IsOnlyFound_TogetherWithItsOwnProduct()
    {
        var (repos, category, product) = await ProductAsync();
        var other = await AddProductAsync(repos, category.CategoryId, "Orangensaft");
        var image = await AddImageAsync(repos, product.ProductId, sortOrder: 0);

        Assert.Null(await repos.ProductImages.GetAsync(other.ProductId, image.ProductImageId));
        Assert.Null(await repos.ProductImages.GetFileAsync(other.ProductId, image.ProductImageId));
        Assert.Null(await repos.ProductImages.GetVisibleFileAsync(other.ProductId, image.ProductImageId));
        Assert.False(await repos.ProductImages.DeleteAsync(other.ProductId, image.ProductImageId));
        Assert.False(await repos.ProductImages.UpdateAsync(new ProductImage
        {
            ProductId = other.ProductId, ProductImageId = image.ProductImageId, Description = "X",
        }));
        Assert.NotNull(await repos.ProductImages.GetAsync(product.ProductId, image.ProductImageId));
    }

    [Fact]
    public async Task AddingAMainImage_TakesTheFlagFromTheOtherImages()
    {
        var (repos, category, product) = await ProductAsync();
        var other = await AddProductAsync(repos, category.CategoryId, "Orangensaft");
        var first = await AddImageAsync(repos, product.ProductId, sortOrder: 0, isPrimary: true);
        var otherMain = await AddImageAsync(repos, other.ProductId, sortOrder: 0, isPrimary: true);

        var second = await AddImageAsync(repos, product.ProductId, sortOrder: 1, isPrimary: true);

        Assert.False((await repos.ProductImages.GetAsync(product.ProductId, first.ProductImageId))!.IsPrimary);
        Assert.True((await repos.ProductImages.GetAsync(product.ProductId, second.ProductImageId))!.IsPrimary);
        Assert.True((await repos.ProductImages.GetAsync(other.ProductId, otherMain.ProductImageId))!.IsPrimary);
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
            ProductImageId = second.ProductImageId,
            Description = null,
            SortOrder = 7,
            IsPrimary = true,
        });

        var loaded = await repos.Products.GetByIdAsync(product.ProductId);
        Assert.True(updated);
        Assert.Equal([first.ProductImageId, second.ProductImageId], loaded!.Images.Select(i => i.ProductImageId));
        Assert.Equal([false, true], loaded.Images.Select(i => i.IsPrimary));
        Assert.Null(loaded.Images.Last().Description);
        Assert.Equal(7, loaded.Images.Last().SortOrder);
        // The file name and the data stay as they are.
        Assert.Equal("bild-1.jpg", loaded.Images.Last().FileName);
        Assert.Equal(ImageData, (await repos.ProductImages.GetFileAsync(product.ProductId, second.ProductImageId))!.Data);
    }

    [Fact]
    public async Task Update_OfAnUnknownImage_KeepsTheCurrentMainImage()
    {
        var (repos, _, product) = await ProductAsync();
        var main = await AddImageAsync(repos, product.ProductId, sortOrder: 0, isPrimary: true);

        var updated = await repos.ProductImages.UpdateAsync(new ProductImage
        {
            ProductId = product.ProductId, ProductImageId = 9999, IsPrimary = true,
        });

        Assert.False(updated);
        Assert.True((await repos.ProductImages.GetAsync(product.ProductId, main.ProductImageId))!.IsPrimary);
    }

    [Fact]
    public async Task Delete_RemovesRecordAndData_AndUnknownReturnsFalse()
    {
        var (repos, _, product) = await ProductAsync();
        var image = await AddImageAsync(repos, product.ProductId, sortOrder: 0);
        var kept = await AddImageAsync(repos, product.ProductId, sortOrder: 1);

        Assert.True(await repos.ProductImages.DeleteAsync(product.ProductId, image.ProductImageId));

        Assert.Null(await repos.ProductImages.GetAsync(product.ProductId, image.ProductImageId));
        Assert.Null(await repos.ProductImages.GetFileAsync(product.ProductId, image.ProductImageId));
        Assert.False(await repos.ProductImages.DeleteAsync(product.ProductId, image.ProductImageId));
        Assert.Equal([kept.ProductImageId], (await repos.Products.GetByIdAsync(product.ProductId))!.Images.Select(i => i.ProductImageId));
    }

    [Fact]
    public async Task VisibleFile_OfAVisibleProduct_IsReturned()
    {
        var (repos, _, product) = await ProductAsync();
        var image = await AddImageAsync(repos, product.ProductId, sortOrder: 0);

        var file = await repos.ProductImages.GetVisibleFileAsync(product.ProductId, image.ProductImageId);

        Assert.Equal(ImageData, file?.Data);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task VisibleFile_OfAnInactiveProduct_OrInAHiddenCategory_IsNull_ButTheAdminAreaGetsIt(
        bool isActive, bool categoryVisible)
    {
        var (repos, _, product) = await ProductAsync(isActive, categoryVisible);
        var image = await AddImageAsync(repos, product.ProductId, sortOrder: 0);

        Assert.Null(await repos.ProductImages.GetVisibleFileAsync(product.ProductId, image.ProductImageId));
        Assert.NotNull(await repos.ProductImages.GetFileAsync(product.ProductId, image.ProductImageId));
    }

    [Fact]
    public async Task UnknownIds_GiveNull()
    {
        var (repos, _, product) = await ProductAsync();

        Assert.Null(await repos.ProductImages.GetAsync(product.ProductId, 9999));
        Assert.Null(await repos.ProductImages.GetFileAsync(9999, 9999));
        Assert.Null(await repos.ProductImages.GetVisibleFileAsync(product.ProductId, 9999));
    }
}
