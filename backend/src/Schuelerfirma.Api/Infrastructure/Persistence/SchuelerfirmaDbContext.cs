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

    /// <summary>Product categories.</summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>Products of the student company.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>All images of the website, without their data and without owner.</summary>
    public DbSet<Image> Images => Set<Image>();

    /// <summary>The stored image data, one row per image.</summary>
    public DbSet<ImageContent> ImageContents => Set<ImageContent>();

    /// <summary>Which images belong to which product, with position and main-image flag.</summary>
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

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