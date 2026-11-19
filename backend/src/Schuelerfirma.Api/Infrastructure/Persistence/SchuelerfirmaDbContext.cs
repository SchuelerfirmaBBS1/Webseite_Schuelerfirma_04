using Microsoft.EntityFrameworkCore;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>
/// Provides access to the application's relational database through Entity Framework Core.
/// </summary>
public sealed class SchuelerfirmaDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchuelerfirmaDbContext"/> class.
    /// </summary>
    /// <param name="options">
    /// The options used to configure the database context.
    /// </param>
    public SchuelerfirmaDbContext(
        DbContextOptions<SchuelerfirmaDbContext> options)
        : base(options)
    {
    }

    /// <summary>Roles available in the administration area.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Users who may log in to the administration area.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Server-side login sessions.</summary>
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();

        configurationBuilder.Properties<DateTime?>()
            .HaveConversion<UtcDateTimeConverter>();
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.HasCharSet("utf8mb4");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SchuelerfirmaDbContext).Assembly);
    }
}