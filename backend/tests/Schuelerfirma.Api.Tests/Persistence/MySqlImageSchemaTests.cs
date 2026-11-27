using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySqlConnector;

namespace Schuelerfirma.Api.Tests.Persistence;

/// <summary>
/// Schema rules of the image model that only a real MySQL can show: an image belongs to at most one product, and
/// the migrations from the first product image tables to images with assignments keep existing images in both
/// directions. Runs in the MySQL collection, so no repository test uses the database meanwhile.
/// </summary>
[Collection(MySqlCollection.Name)]
public sealed class MySqlImageSchemaTests(MySqlContainerFixture fixture) : IAsyncLifetime
{
    private const string FirstProductImageTables = "20261127095328_AddProductCatalog";

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();

    // Leave the database on the latest migration for the other tests, whatever happened here.
    public async Task DisposeAsync()
    {
        await using var db = await fixture.ContextFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
    }

    [Fact]
    public async Task ProductImages_AreKeptByAllMigrations_UpAndDown()
    {
        await using var db = await fixture.ContextFactory.CreateDbContextAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(FirstProductImageTables);

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO `Categories` (`CategoryId`, `Name`, `IsVisible`) VALUES (11, 'Getränke', 1);" +
            "INSERT INTO `Products` (`ProductId`, `CategoryId`, `Name`, `IsPriceVisible`, `IsActive`) VALUES (21, 11, 'Saft', 1, 1);" +
            "INSERT INTO `ProductImages` (`ProductImageId`, `ProductId`, `FileName`, `ContentType`, `Length`, `SortOrder`, `IsPrimary`) " +
            "VALUES (31, 21, 'saft.jpg', 'image/jpeg', 3, 2, 1);" +
            "INSERT INTO `ProductImageContents` (`ProductImageId`, `Data`) VALUES (31, 0x010203);");

        await migrator.MigrateAsync();

        var image = await db.Images.AsNoTracking().SingleAsync();
        var content = await db.ImageContents.AsNoTracking().SingleAsync();
        var assignment = await db.ProductImages.AsNoTracking().SingleAsync();
        Assert.Equal((31, "saft.jpg", "image/jpeg", 3L), (image.ImageId, image.FileName, image.ContentType, image.Length));
        Assert.Equal(31, content.ImageId);
        Assert.Equal([1, 2, 3], content.Data);
        Assert.Equal((31, 21, 2, true), (assignment.ImageId, assignment.ProductId, assignment.SortOrder, assignment.IsPrimary));

        await migrator.MigrateAsync(FirstProductImageTables);

        var back = await db.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS `Value` FROM `ProductImages` i JOIN `ProductImageContents` c ON c.`ProductImageId` = i.`ProductImageId` " +
                "WHERE i.`ProductImageId` = 31 AND i.`ProductId` = 21 AND i.`SortOrder` = 2 AND i.`IsPrimary` = 1 AND c.`Data` = 0x010203")
            .SingleAsync();
        Assert.Equal(1, back);
    }

    [Fact]
    public async Task AnImage_CannotBeAssignedToTwoProducts()
    {
        await using var db = await fixture.ContextFactory.CreateDbContextAsync();
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO `Categories` (`CategoryId`, `Name`, `IsVisible`) VALUES (11, 'Getränke', 1);" +
            "INSERT INTO `Products` (`ProductId`, `CategoryId`, `Name`, `IsPriceVisible`, `IsActive`) VALUES (21, 11, 'Saft', 1, 1), (22, 11, 'Tee', 1, 1);" +
            "INSERT INTO `Images` (`ImageId`, `FileName`, `ContentType`, `Length`) VALUES (31, 'saft.jpg', 'image/jpeg', 3);" +
            "INSERT INTO `ProductImages` (`ImageId`, `ProductId`, `SortOrder`, `IsPrimary`) VALUES (31, 21, 0, 1);");

        var error = await Assert.ThrowsAsync<MySqlException>(() => db.Database.ExecuteSqlRawAsync(
            "INSERT INTO `ProductImages` (`ImageId`, `ProductId`, `SortOrder`, `IsPrimary`) VALUES (31, 22, 0, 1);"));

        Assert.Equal(MySqlErrorCode.DuplicateKeyEntry, error.ErrorCode);
    }
}
