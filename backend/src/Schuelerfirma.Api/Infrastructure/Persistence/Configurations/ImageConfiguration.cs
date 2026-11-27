using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence.Configurations;

/// <summary>Configures the database mapping for <see cref="Image"/>; owners refer to it, not the other way round.</summary>
public sealed class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Image> builder)
    {
        builder.ToTable("Images");

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
    }
}
