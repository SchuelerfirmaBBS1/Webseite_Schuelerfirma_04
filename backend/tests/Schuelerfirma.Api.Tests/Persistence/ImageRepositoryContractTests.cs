using Schuelerfirma.Api.Application.Images;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Tests.Persistence;

public abstract class ImageRepositoryContractTests : RepositoryContractTests
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
        var loaded = await repos.Images.GetAsync(ImageOwner.Product(product.ProductId), image.ImageId);
        var file = await repos.Images.GetFileAsync(ImageOwner.Product(product.ProductId), image.ImageId);

        Assert.True(image.ImageId > 0);
        Assert.Equal(("bild-3.jpg", "Bild 3", "image/jpeg", (long)ImageData.Length, 3, true),
            (loaded!.FileName, loaded.Description, loaded.ContentType, loaded.Length, loaded.SortOrder, loaded.IsPrimary));
        Assert.Equal("image/jpeg", file!.ContentType);
        Assert.Equal(ImageData, file.Data);
    }

    [Fact]
    public async Task Add_WithoutOwner_Throws_AndNoOwnerFindsNothing()
    {
        // The database checks "exactly one owner" as well (CK_Images_ExactlyOneOwner); the repositories reject it
        // before, with an exception that names the problem.
        var (repos, _, product) = await ProductAsync();
        var stored = await AddImageAsync(repos, product.ProductId, sortOrder: 0);
        var withoutOwner = NewImage(product.ProductId, sortOrder: 1);
        withoutOwner.ProductId = null;

        await Assert.ThrowsAsync<ArgumentException>(() => repos.Images.AddAsync(withoutOwner, ImageData));
        Assert.Null(await repos.Images.GetAsync(new ImageOwner(null), stored.ImageId));
        Assert.Null(await repos.Images.GetFileAsync(new ImageOwner(null), stored.ImageId));
        Assert.False(await repos.Images.DeleteAsync(new ImageOwner(null), stored.ImageId));
        Assert.Equal([stored.ImageId], (await repos.Products.GetByIdAsync(product.ProductId))!.Images.Select(i => i.ImageId));
    }

    [Fact]
    public async Task Add_ToUnknownProduct_Throws()
    {
        var repos = CreateRepositories();

        await Assert.ThrowsAsync<InvalidOperationException>(() => repos.Images.AddAsync(NewImage(9999, 0), ImageData));
    }

    [Fact]
    public async Task AnImage_IsOnlyFound_TogetherWithItsOwnProduct()
    {
        var (repos, category, product) = await ProductAsync();
        var other = await AddProductAsync(repos, category.CategoryId, "Orangensaft");
        var image = await AddImageAsync(repos, product.ProductId, sortOrder: 0);

        Assert.Null(await repos.Images.GetAsync(ImageOwner.Product(other.ProductId), image.ImageId));
        Assert.Null(await repos.Images.GetFileAsync(ImageOwner.Product(other.ProductId), image.ImageId));
        Assert.Null(await repos.Images.GetVisibleFileAsync(ImageOwner.Product(other.ProductId), image.ImageId));
        Assert.False(await repos.Images.DeleteAsync(ImageOwner.Product(other.ProductId), image.ImageId));
        Assert.False(await repos.Images.UpdateAsync(new Image
        {
            ProductId = other.ProductId, ImageId = image.ImageId, Description = "X",
        }));
        Assert.NotNull(await repos.Images.GetAsync(ImageOwner.Product(product.ProductId), image.ImageId));
    }

    [Fact]
    public async Task AddingAMainImage_TakesTheFlagFromTheOtherImages()
    {
        var (repos, category, product) = await ProductAsync();
        var other = await AddProductAsync(repos, category.CategoryId, "Orangensaft");
        var first = await AddImageAsync(repos, product.ProductId, sortOrder: 0, isPrimary: true);
        var otherMain = await AddImageAsync(repos, other.ProductId, sortOrder: 0, isPrimary: true);

        var second = await AddImageAsync(repos, product.ProductId, sortOrder: 1, isPrimary: true);

        Assert.False((await repos.Images.GetAsync(ImageOwner.Product(product.ProductId), first.ImageId))!.IsPrimary);
        Assert.True((await repos.Images.GetAsync(ImageOwner.Product(product.ProductId), second.ImageId))!.IsPrimary);
        Assert.True((await repos.Images.GetAsync(ImageOwner.Product(other.ProductId), otherMain.ImageId))!.IsPrimary);
    }

    [Fact]
    public async Task Update_ChangesDescriptionOrderAndMainImage_AndLeavesAtMostOneMainImage()
    {
        var (repos, _, product) = await ProductAsync();
        var first = await AddImageAsync(repos, product.ProductId, sortOrder: 0, isPrimary: true);
        var second = await AddImageAsync(repos, product.ProductId, sortOrder: 1);

        var updated = await repos.Images.UpdateAsync(new Image
        {
            ProductId = product.ProductId,
            ImageId = second.ImageId,
            Description = null,
            SortOrder = 7,
            IsPrimary = true,
        });

        var loaded = await repos.Products.GetByIdAsync(product.ProductId);
        Assert.True(updated);
        Assert.Equal([first.ImageId, second.ImageId], loaded!.Images.Select(i => i.ImageId));
        Assert.Equal([false, true], loaded.Images.Select(i => i.IsPrimary));
        Assert.Null(loaded.Images.Last().Description);
        Assert.Equal(7, loaded.Images.Last().SortOrder);
        // The file name and the data stay as they are.
        Assert.Equal("bild-1.jpg", loaded.Images.Last().FileName);
        Assert.Equal(ImageData, (await repos.Images.GetFileAsync(ImageOwner.Product(product.ProductId), second.ImageId))!.Data);
    }

    [Fact]
    public async Task Update_OfAnUnknownImage_KeepsTheCurrentMainImage()
    {
        var (repos, _, product) = await ProductAsync();
        var main = await AddImageAsync(repos, product.ProductId, sortOrder: 0, isPrimary: true);

        var updated = await repos.Images.UpdateAsync(new Image
        {
            ProductId = product.ProductId, ImageId = 9999, IsPrimary = true,
        });

        Assert.False(updated);
        Assert.True((await repos.Images.GetAsync(ImageOwner.Product(product.ProductId), main.ImageId))!.IsPrimary);
    }

    [Fact]
    public async Task Delete_RemovesRecordAndData_AndUnknownReturnsFalse()
    {
        var (repos, _, product) = await ProductAsync();
        var image = await AddImageAsync(repos, product.ProductId, sortOrder: 0);
        var kept = await AddImageAsync(repos, product.ProductId, sortOrder: 1);

        Assert.True(await repos.Images.DeleteAsync(ImageOwner.Product(product.ProductId), image.ImageId));

        Assert.Null(await repos.Images.GetAsync(ImageOwner.Product(product.ProductId), image.ImageId));
        Assert.Null(await repos.Images.GetFileAsync(ImageOwner.Product(product.ProductId), image.ImageId));
        Assert.False(await repos.Images.DeleteAsync(ImageOwner.Product(product.ProductId), image.ImageId));
        Assert.Equal([kept.ImageId], (await repos.Products.GetByIdAsync(product.ProductId))!.Images.Select(i => i.ImageId));
    }

    [Fact]
    public async Task VisibleFile_OfAVisibleProduct_IsReturned()
    {
        var (repos, _, product) = await ProductAsync();
        var image = await AddImageAsync(repos, product.ProductId, sortOrder: 0);

        var file = await repos.Images.GetVisibleFileAsync(ImageOwner.Product(product.ProductId), image.ImageId);

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

        Assert.Null(await repos.Images.GetVisibleFileAsync(ImageOwner.Product(product.ProductId), image.ImageId));
        Assert.NotNull(await repos.Images.GetFileAsync(ImageOwner.Product(product.ProductId), image.ImageId));
    }

    [Fact]
    public async Task UnknownIds_GiveNull()
    {
        var (repos, _, product) = await ProductAsync();

        Assert.Null(await repos.Images.GetAsync(ImageOwner.Product(product.ProductId), 9999));
        Assert.Null(await repos.Images.GetFileAsync(ImageOwner.Product(9999), 9999));
        Assert.Null(await repos.Images.GetVisibleFileAsync(ImageOwner.Product(product.ProductId), 9999));
    }
}
