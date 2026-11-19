using Microsoft.EntityFrameworkCore;
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
    }

    public Repositories CreateRepositories() =>
        new(
            new EfUserRepository(ContextFactory),
            new EfRoleRepository(ContextFactory),
            new EfUserSessionRepository(ContextFactory));

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
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
