using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySqlConnector;
using Schuelerfirma.Api.Infrastructure.Persistence.Configurations;

namespace Schuelerfirma.Api.Tests.Persistence;

/// <summary>
/// Schema rules of the one image model that only a real MySQL can show: the check constraint for the owner and the
/// migration from the separate product image tables, which keeps existing images in both directions. Runs in the
/// MySQL collection, so no repository test uses the database meanwhile.
/// </summary>
[Collection(MySqlCollection.Name)]
public sealed class MySqlImageSchemaTests(MySqlContainerFixture fixture) : IAsyncLifetime
{
    private const string BeforeOneImageModel = "20261127095328_AddProductCatalog";

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();

    // Leave the database on the latest migration for the other tests, whatever happened here.
    public async Task DisposeAsync()
    {
        await using var db = await fixture.ContextFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
    }

    [Fact]
    public async Task ProductImages_AreCopiedIntoImages_AndBackAgain()
    {
        await using var db = await fixture.ContextFactory.CreateDbContextAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(BeforeOneImageModel);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO `Categories` (`CategoryId`, `Name`, `IsVisible`) VALUES (11, 'Getränke', 1);" +
            "INSERT INTO `Products` (`ProductId`, `CategoryId`, `Name`, `IsPriceVisible`, `IsActive`) VALUES (21, 11, 'Saft', 1, 1);" +
            "INSERT INTO `ProductImages` (`ProductImageId`, `ProductId`, `FileName`, `ContentType`, `Length`, `SortOrder`, `IsPrimary`) " +
            "VALUES (31, 21, 'saft.jpg', 'image/jpeg', 3, 2, 1);" +
            "INSERT INTO `ProductImageContents` (`ProductImageId`, `Data`) VALUES (31, 0x010203);");

        await migrator.MigrateAsync();

        var image = await db.Images.AsNoTracking().SingleAsync();
        var content = await db.ImageContents.AsNoTracking().SingleAsync();
        Assert.Equal((31, 21, "saft.jpg", "image/jpeg", 3L, 2, true),
            (image.ImageId, image.ProductId, image.FileName, image.ContentType, image.Length, image.SortOrder, image.IsPrimary));
        Assert.Equal(31, content.ImageId);
        Assert.Equal([1, 2, 3], content.Data);

        await migrator.MigrateAsync(BeforeOneImageModel);

        var back = await db.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS `Value` FROM `ProductImages` i JOIN `ProductImageContents` c ON c.`ProductImageId` = i.`ProductImageId` WHERE i.`ProductImageId` = 31 AND i.`ProductId` = 21 AND c.`Data` = 0x010203")
            .SingleAsync();
        Assert.Equal(1, back);
    }

    [Fact]
    public async Task AnImageWithoutOwner_IsRejectedByTheDatabase()
    {
        await using var db = await fixture.ContextFactory.CreateDbContextAsync();

        var error = await Assert.ThrowsAsync<MySqlException>(() => db.Database.ExecuteSqlRawAsync(
            "INSERT INTO `Images` (`ProductId`, `FileName`, `ContentType`, `Length`, `SortOrder`, `IsPrimary`) " +
            "VALUES (NULL, 'x.jpg', 'image/jpeg', 1, 0, 0);"));

        Assert.Contains(ImageConfiguration.ExactlyOneOwnerConstraint, error.Message);
    }
}
