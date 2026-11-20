using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence.Configurations;

/// <summary>Configures the database mapping for <see cref="ProductImage"/>.</summary>
public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");

        builder.HasKey(image => image.ProductImageId);

        builder.Property(image => image.ProductImageId)
            .ValueGeneratedOnAdd();

        builder.Property(image => image.FileName)
            .HasMaxLength(ProductImage.FileNameMaxLength)
            .IsRequired();

        builder.Property(image => image.Description)
            .HasMaxLength(ProductImage.DescriptionMaxLength)
            .IsRequired(false);

        builder.Property(image => image.ContentType)
            .HasMaxLength(ProductImage.ContentTypeMaxLength)
            .IsRequired();

        builder.Property(image => image.FilePath)
            .HasMaxLength(ProductImage.FilePathMaxLength)
            .IsRequired();

        builder.Property(image => image.SortOrder)
            .IsRequired();

        builder.Property(image => image.IsPrimary)
            .IsRequired();

        builder.HasIndex(image => image.ProductId)
            .HasDatabaseName("IX_ProductImages_ProductId");

        builder.HasOne(image => image.Product)
            .WithMany(product => product.Images)
            .HasForeignKey(image => image.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
