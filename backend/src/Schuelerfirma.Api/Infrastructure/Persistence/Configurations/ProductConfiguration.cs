using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence.Configurations;

/// <summary>Configures the database mapping for <see cref="Product"/>.</summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(product => product.ProductId);

        builder.Property(product => product.ProductId)
            .ValueGeneratedOnAdd();

        // Case-insensitive like the in-memory repository, so both sort the lists the same way.
        builder.Property(product => product.Name)
            .HasMaxLength(Product.NameMaxLength)
            .UseCollation(Collations.CaseInsensitive)
            .IsRequired();

        builder.Property(product => product.Description)
            .HasMaxLength(Product.DescriptionMaxLength)
            .IsRequired(false);

        builder.Property(product => product.Price)
            .HasPrecision(10, Product.PriceDecimalPlaces)
            .IsRequired(false);

        builder.Property(product => product.IsPriceVisible)
            .IsRequired();

        builder.Property(product => product.IsActive)
            .IsRequired();

        builder.HasIndex(product => product.CategoryId)
            .HasDatabaseName("IX_Products_CategoryId");

        // Restrict: a category that still has products cannot be deleted (MySQL error 1451).
        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
