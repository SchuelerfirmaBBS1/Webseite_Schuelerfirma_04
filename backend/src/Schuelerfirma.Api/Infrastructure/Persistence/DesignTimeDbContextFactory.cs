using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>
/// Creates a <see cref="SchuelerfirmaDbContext"/> at design time for EF Core CLI tools.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SchuelerfirmaDbContext>
{
    /// <inheritdoc />
    public SchuelerfirmaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SchuelerfirmaDbContext>()
            .UseMySql(
                "Server=localhost;Database=schuelerfirma_design",
                new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;

        return new SchuelerfirmaDbContext(options);
    }
}
