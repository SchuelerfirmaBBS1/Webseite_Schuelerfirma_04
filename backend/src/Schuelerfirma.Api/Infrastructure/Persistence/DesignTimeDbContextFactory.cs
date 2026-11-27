using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>
/// Creates a <see cref="SchuelerfirmaDbContext"/> at design time for EF Core CLI tools.
/// </summary>
/// <remarks>
/// The connection string comes from <c>ConnectionStrings:Default</c> like in the application: user-secrets
/// locally, the environment variable <c>ConnectionStrings__Default</c> on a server. <c>--connection</c> of
/// <c>dotnet ef</c> still takes precedence. Without a connection string a placeholder is used, which is
/// enough for <c>migrations add</c> (that command opens no connection), but not for <c>database update</c>.
/// </remarks>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SchuelerfirmaDbContext>
{
    private const string PlaceholderConnectionString = "Server=localhost;Database=schuelerfirma_design";

    /// <inheritdoc />
    public SchuelerfirmaDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<DesignTimeDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = PlaceholderConnectionString;
        }

        var options = new DbContextOptionsBuilder<SchuelerfirmaDbContext>()
            .UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;

        return new SchuelerfirmaDbContext(options);
    }
}
