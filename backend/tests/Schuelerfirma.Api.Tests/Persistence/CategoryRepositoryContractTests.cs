using Schuelerfirma.Api.Application.Abstractions;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Tests.Persistence;

public abstract class CategoryRepositoryContractTests : RepositoryContractTests
{
    [Fact]
    public async Task Add_AssignsId_AndGetByIdReturnsTheCategory()
    {
        var repos = CreateRepositories();

        var category = await AddCategoryAsync(repos, "Getränke", isVisible: false);
        var loaded = await repos.Categories.GetByIdAsync(category.CategoryId);

        Assert.True(category.CategoryId > 0);
        Assert.Equal("Getränke", loaded?.Name);
        Assert.False(loaded!.IsVisible);
        Assert.Null(await repos.Categories.GetByIdAsync(9999));
    }

    [Fact]
    public async Task GetAll_IsOrderedByName_IgnoringCase()
    {
        var repos = CreateRepositories();
        await AddCategoryAsync(repos, "snacks");
        await AddCategoryAsync(repos, "Getränke", isVisible: false);
        await AddCategoryAsync(repos, "Bastelbedarf");

        var categories = await repos.Categories.GetAllAsync();

        Assert.Equal(["Bastelbedarf", "Getränke", "snacks"], categories.Select(c => c.Name));
    }

    [Fact]
    public async Task GetVisible_ReturnsOnlyVisibleCategories_OrderedByName()
    {
        var repos = CreateRepositories();
        await AddCategoryAsync(repos, "Snacks");
        await AddCategoryAsync(repos, "Geheim", isVisible: false);
        await AddCategoryAsync(repos, "Bastelbedarf");

        var categories = await repos.Categories.GetVisibleAsync();

        Assert.Equal(["Bastelbedarf", "Snacks"], categories.Select(c => c.Name));
    }

    [Fact]
    public async Task Add_DuplicateName_IgnoringCase_Throws()
    {
        var repos = CreateRepositories();
        await AddCategoryAsync(repos, "Getränke");

        var exception = await Assert.ThrowsAsync<DuplicateEntryException>(() => AddCategoryAsync(repos, "GETRÄNKE"));

        Assert.Equal(nameof(Category.Name), exception.Field);
    }

    [Fact]
    public async Task Update_ChangesNameAndVisibility()
    {
        var repos = CreateRepositories();
        var category = await AddCategoryAsync(repos, "Getränke");

        await repos.Categories.UpdateAsync(new Category { CategoryId = category.CategoryId, Name = "Kaltgetränke", IsVisible = false });
        var loaded = await repos.Categories.GetByIdAsync(category.CategoryId);

        Assert.Equal("Kaltgetränke", loaded?.Name);
        Assert.False(loaded!.IsVisible);
    }

    [Fact]
    public async Task Update_KeepingTheOwnName_InOtherCase_IsAllowed()
    {
        var repos = CreateRepositories();
        var category = await AddCategoryAsync(repos, "getränke");

        await repos.Categories.UpdateAsync(new Category { CategoryId = category.CategoryId, Name = "Getränke", IsVisible = true });

        Assert.Equal("Getränke", (await repos.Categories.GetByIdAsync(category.CategoryId))?.Name);
    }

    [Fact]
    public async Task Update_NameOfAnotherCategory_Throws()
    {
        var repos = CreateRepositories();
        await AddCategoryAsync(repos, "Getränke");
        var snacks = await AddCategoryAsync(repos, "Snacks");

        await Assert.ThrowsAsync<DuplicateEntryException>(() =>
            repos.Categories.UpdateAsync(new Category { CategoryId = snacks.CategoryId, Name = "getränke", IsVisible = true }));
    }

    [Fact]
    public async Task Update_Unknown_Throws()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateRepositories().Categories.UpdateAsync(new Category { CategoryId = 9999, Name = "X", IsVisible = true }));
    }

    [Fact]
    public async Task Delete_EmptyCategory_Deletes_AndUnknownIsNotFound()
    {
        var repos = CreateRepositories();
        var category = await AddCategoryAsync(repos);

        Assert.Equal(CategoryDeleteResult.Deleted, await repos.Categories.DeleteAsync(category.CategoryId));
        Assert.Null(await repos.Categories.GetByIdAsync(category.CategoryId));
        Assert.Equal(CategoryDeleteResult.NotFound, await repos.Categories.DeleteAsync(category.CategoryId));
    }

    [Fact]
    public async Task Delete_CategoryWithProducts_KeepsIt()
    {
        var repos = CreateRepositories();
        var category = await AddCategoryAsync(repos);
        await AddProductAsync(repos, category.CategoryId);

        Assert.Equal(CategoryDeleteResult.HasProducts, await repos.Categories.DeleteAsync(category.CategoryId));
        Assert.NotNull(await repos.Categories.GetByIdAsync(category.CategoryId));
    }
}
