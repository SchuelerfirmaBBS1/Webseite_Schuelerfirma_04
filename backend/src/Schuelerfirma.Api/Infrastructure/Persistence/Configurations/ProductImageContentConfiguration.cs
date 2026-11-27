using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence.Configurations;

/// <summary>Configures the database mapping for <see cref="ProductImageContent"/>.</summary>
public sealed class ProductImageContentConfiguration : IEntityTypeConfiguration<ProductImageContent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProductImageContent> builder)
    {
        builder.ToTable("ProductImageContents");

        builder.HasKey(content => content.ProductImageId);

        builder.Property(content => content.ProductImageId)
            .ValueGeneratedNever();

        builder.Property(content => content.Data)
            .HasColumnType("mediumblob")
            .IsRequired();

        // No navigation on ProductImage: the data is only ever loaded on its own, never with an Include.
        builder.HasOne<ProductImage>()
            .WithOne()
            .HasForeignKey<ProductImageContent>(content => content.ProductImageId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
