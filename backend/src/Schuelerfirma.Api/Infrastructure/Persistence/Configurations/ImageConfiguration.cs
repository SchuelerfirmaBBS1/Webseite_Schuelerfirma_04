using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence.Configurations;

/// <summary>Configures the database mapping for <see cref="Image"/>.</summary>
public sealed class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    /// <summary>Name of the check constraint that allows exactly one owner per image.</summary>
    public const string ExactlyOneOwnerConstraint = "CK_Images_ExactlyOneOwner";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Image> builder)
    {
        // Exactly one owner. Each owner adds "+ (`<Owner>Id` IS NOT NULL)"; the events (#14) add EventId.
        builder.ToTable("Images", table => table.HasCheckConstraint(
            ExactlyOneOwnerConstraint, "(`ProductId` IS NOT NULL) = 1"));

        builder.HasKey(image => image.ImageId);

        builder.Property(image => image.ImageId)
            .ValueGeneratedOnAdd();

        builder.Property(image => image.FileName)
            .HasMaxLength(Image.FileNameMaxLength)
            .IsRequired();

        builder.Property(image => image.Description)
            .HasMaxLength(Image.DescriptionMaxLength)
            .IsRequired(false);

        builder.Property(image => image.ContentType)
            .HasMaxLength(Image.ContentTypeMaxLength)
            .IsRequired();

        builder.Property(image => image.Length)
            .IsRequired();

        builder.Property(image => image.SortOrder)
            .IsRequired();

        builder.Property(image => image.IsPrimary)
            .IsRequired();

        builder.HasIndex(image => image.ProductId)
            .HasDatabaseName("IX_Images_ProductId");

        // Optional, because an image can belong to another owner; the check constraint above requires one.
        builder.HasOne(image => image.Product)
            .WithMany(product => product.Images)
            .HasForeignKey(image => image.ProductId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
