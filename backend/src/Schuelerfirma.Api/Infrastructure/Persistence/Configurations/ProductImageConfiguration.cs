using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence.Configurations;

/// <summary>Configures the database mapping for <see cref="ProductImage"/>, the assignment of images to products.</summary>
public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");

        // The image is the key: an image belongs to at most one product.
        builder.HasKey(link => link.ImageId);

        builder.Property(link => link.ImageId)
            .ValueGeneratedNever();

        builder.Property(link => link.SortOrder)
            .IsRequired();

        builder.Property(link => link.IsPrimary)
            .IsRequired();

        builder.HasIndex(link => link.ProductId)
            .HasDatabaseName("IX_ProductImages_ProductId");

        // Deleting the product only removes the assignment; EfProductRepository deletes the images first.
        builder.HasOne(link => link.Product)
            .WithMany(product => product.Images)
            .HasForeignKey(link => link.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Deleting an image removes its assignment.
        builder.HasOne(link => link.Image)
            .WithOne()
            .HasForeignKey<ProductImage>(link => link.ImageId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
