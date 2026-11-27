using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Schuelerfirma.Api.Domain;
using Schuelerfirma.Api.Infrastructure.Persistence;
using Testcontainers.MySql;
using Xunit;

namespace Schuelerfirma.Api.Tests.Persistence;

/// <summary>
/// Starts a single MySQL 8.4 container for all repository contract tests in this collection.
/// </summary>
public sealed class MySqlContainerFixture : IAsyncLifetime
{
    private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.4")
        .WithDatabase("schuelerfirma_test")
        .WithUsername("testuser")
        .WithPassword("testpassword")
        // The module's startup callback and its default wait strategy both use "docker exec" (mkdir, mysql
        // client). Under load, rootless Podman resets that exec stream ("Connection reset by peer"). The
        // tests need neither: the official image creates the directories itself, and readiness is checked
        // with a real login from the host.
        .WithStartupCallback((_, _) => Task.CompletedTask)
        .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new UntilLoginSucceeds()))
        .Build();

    public IDbContextFactory<SchuelerfirmaDbContext> ContextFactory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<SchuelerfirmaDbContext>()
            .UseMySql(
                _container.GetConnectionString(),
                new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;

        ContextFactory = new SimpleDbContextFactory(options);

        await using var db = await ContextFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var db = await ContextFactory.CreateDbContextAsync();
        await db.UserSessions.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
        await db.ProductImageContents.ExecuteDeleteAsync();
        await db.ProductImages.ExecuteDeleteAsync();
        await db.Products.ExecuteDeleteAsync();
        await db.Categories.ExecuteDeleteAsync();
    }

    public Repositories CreateRepositories() =>
        new(
            new EfUserRepository(ContextFactory),
            new EfRoleRepository(ContextFactory),
            new EfUserSessionRepository(ContextFactory),
            new EfCategoryRepository(ContextFactory),
            new EfProductRepository(ContextFactory),
            new EfProductImageRepository(ContextFactory));

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    /// <summary>Ready once the test user can log in to the test database through the mapped port.</summary>
    private sealed class UntilLoginSucceeds : IWaitUntil
    {
        public async Task<bool> UntilAsync(IContainer container)
        {
            try
            {
                await using var connection = new MySqlConnection(((MySqlContainer)container).GetConnectionString());
                await connection.OpenAsync();
                return true;
            }
            catch (Exception exception) when (exception is MySqlException or IOException or InvalidOperationException)
            {
                return false;
            }
        }
    }

    private sealed class SimpleDbContextFactory(DbContextOptions<SchuelerfirmaDbContext> options)
        : IDbContextFactory<SchuelerfirmaDbContext>
    {
        public SchuelerfirmaDbContext CreateDbContext() => new(options);
    }
}

[CollectionDefinition(Name)]
public sealed class MySqlCollection : ICollectionFixture<MySqlContainerFixture>
{
    public const string Name = "MySQL Repository Tests";
}

[Collection(MySqlCollection.Name)]
public sealed class MySqlRoleRepositoryTests(MySqlContainerFixture fixture)
    : RoleRepositoryContractTests, IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    protected override Repositories CreateRepositories() => fixture.CreateRepositories();
}

[Collection(MySqlCollection.Name)]
public sealed class MySqlUserRepositoryTests(MySqlContainerFixture fixture)
    : UserRepositoryContractTests, IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    protected override Repositories CreateRepositories() => fixture.CreateRepositories();
}

[Collection(MySqlCollection.Name)]
public sealed class MySqlUserSessionRepositoryTests(MySqlContainerFixture fixture)
    : UserSessionRepositoryContractTests, IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    protected override Repositories CreateRepositories() => fixture.CreateRepositories();
}

[Collection(MySqlCollection.Name)]
public sealed class MySqlCategoryRepositoryTests(MySqlContainerFixture fixture)
    : CategoryRepositoryContractTests, IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    protected override Repositories CreateRepositories() => fixture.CreateRepositories();
}

[Collection(MySqlCollection.Name)]
public sealed class MySqlProductRepositoryTests(MySqlContainerFixture fixture)
    : ProductRepositoryContractTests, IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    protected override Repositories CreateRepositories() => fixture.CreateRepositories();
}

[Collection(MySqlCollection.Name)]
public sealed class MySqlProductImageRepositoryTests(MySqlContainerFixture fixture)
    : ProductImageRepositoryContractTests, IAsyncLifetime
{
    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    protected override Repositories CreateRepositories() => fixture.CreateRepositories();
}
