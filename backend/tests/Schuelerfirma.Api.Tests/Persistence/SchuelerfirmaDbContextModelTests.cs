using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Metadata;
using Schuelerfirma.Api.Domain;
using Schuelerfirma.Api.Infrastructure.Persistence;

namespace Schuelerfirma.Api.Tests.Infrastructure.Persistence;

public sealed class SchuelerfirmaDbContextModelTests
{
    // Opens no connection: only the model is read.
    private static SchuelerfirmaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<SchuelerfirmaDbContext>()
            .UseMySql(
                "Server=localhost;Database=model_test",
                new MySqlServerVersion(new Version(8, 4, 0)))
            .Options);

    private static Microsoft.EntityFrameworkCore.Metadata.IModel GetModel(
        SchuelerfirmaDbContext context) =>
        context.GetService<IDesignTimeModel>().Model;

    [Fact]
    public void Model_contains_expected_entities()
    {
        using var context = CreateContext();
        var model = GetModel(context);

        Assert.NotNull(model.FindEntityType(typeof(Role)));
        Assert.NotNull(model.FindEntityType(typeof(User)));
        Assert.NotNull(model.FindEntityType(typeof(UserSession)));
        Assert.NotNull(model.FindEntityType(typeof(Category)));
        Assert.NotNull(model.FindEntityType(typeof(Product)));
        Assert.NotNull(model.FindEntityType(typeof(ProductImage)));
    }

    [Fact]
    public void Product_has_required_category_foreign_key_with_restrict()
    {
        using var context = CreateContext();
        var foreignKey = GetModel(context).FindEntityType(typeof(Product))!
            .GetForeignKeys()
            .Single(key => key.PrincipalEntityType.ClrType == typeof(Category));

        Assert.True(foreignKey.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void ProductImage_is_deleted_with_product()
    {
        using var context = CreateContext();
        var foreignKey = GetModel(context).FindEntityType(typeof(ProductImage))!
            .GetForeignKeys()
            .Single(key => key.PrincipalEntityType.ClrType == typeof(Product));

        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void Product_price_has_two_decimal_places()
    {
        using var context = CreateContext();
        var price = GetModel(context).FindEntityType(typeof(Product))!.FindProperty(nameof(Product.Price))!;

        Assert.Equal(10, price.GetPrecision());
        Assert.Equal(2, price.GetScale());
        Assert.True(price.IsNullable);
    }

    [Fact]
    public void User_has_required_role_foreign_key_with_restrict()
    {
        using var context = CreateContext();
        var foreignKey = GetModel(context).FindEntityType(typeof(User))!
            .GetForeignKeys()
            .Single(key => key.PrincipalEntityType.ClrType == typeof(Role));

        Assert.True(foreignKey.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void UserSession_is_deleted_with_user()
    {
        using var context = CreateContext();
        var foreignKey = GetModel(context).FindEntityType(typeof(UserSession))!
            .GetForeignKeys()
            .Single(key => key.PrincipalEntityType.ClrType == typeof(User));

        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    [Theory]
    [InlineData(typeof(Role), nameof(Role.Name))]
    [InlineData(typeof(User), nameof(User.Username))]
    [InlineData(typeof(User), nameof(User.Email))]
    [InlineData(typeof(Category), nameof(Category.Name))]
    public void Unique_text_columns_use_case_insensitive_collation(
        Type entityType, string propertyName)
    {
        using var context = CreateContext();
        var property = GetModel(context).FindEntityType(entityType)!
            .FindProperty(propertyName)!;

        Assert.Equal("utf8mb4_0900_as_ci", property.GetCollation());
        Assert.Contains(
            property.GetContainingIndexes(),
            index => index.IsUnique);
    }

    [Fact]
    public void Unique_indexes_have_the_names_the_repositories_rely_on()
    {
        using var context = CreateContext();
        var userEntity = GetModel(context).FindEntityType(typeof(User))!;

        var names = userEntity.GetIndexes()
            .Where(index => index.IsUnique)
            .Select(index => index.GetDatabaseName())
            .ToList();

        Assert.Contains("UX_Users_Username", names);
        Assert.Contains("UX_Users_Email", names);
        Assert.Contains(
            "UX_Categories_Name",
            GetModel(context).FindEntityType(typeof(Category))!.GetIndexes().Where(index => index.IsUnique).Select(index => index.GetDatabaseName()));
    }

    [Fact]
    public void Bool_columns_have_no_database_default()
    {
        using var context = CreateContext();
        var userEntity = GetModel(context).FindEntityType(typeof(User))!;

        Assert.Null(userEntity.FindProperty(nameof(User.IsActive))!
            .FindAnnotation(RelationalAnnotationNames.DefaultValue));
        Assert.Null(userEntity.FindProperty(nameof(User.MustChangePassword))!
            .FindAnnotation(RelationalAnnotationNames.DefaultValue));
    }

    [Theory]
    [InlineData(typeof(User), nameof(User.CreatedAt))]
    [InlineData(typeof(User), nameof(User.LastLoginAt))]
    [InlineData(typeof(User), nameof(User.LockoutEnd))]
    [InlineData(typeof(UserSession), nameof(UserSession.CreatedAt))]
    [InlineData(typeof(UserSession), nameof(UserSession.ExpiresAt))]
    public void DateTime_values_are_read_back_as_utc(Type entityType, string propertyName)
    {
        using var context = CreateContext();
        var converter = GetModel(context).FindEntityType(entityType)!
            .FindProperty(propertyName)!
            .GetValueConverter();

        Assert.NotNull(converter);

        var fromDatabase = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
        var result = (DateTime)converter!.ConvertFromProvider(fromDatabase)!;

        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void Roles_are_seeded_from_DefaultRoles()
    {
        using var context = CreateContext();
        var seed = GetModel(context).FindEntityType(typeof(Role))!.GetSeedData().ToList();

        Assert.Equal(DefaultRoles.CreateSeed().Count(), seed.Count);
    }
}