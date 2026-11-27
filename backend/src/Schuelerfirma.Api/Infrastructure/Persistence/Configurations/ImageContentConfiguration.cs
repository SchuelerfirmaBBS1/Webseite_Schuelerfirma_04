using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence.Configurations;

/// <summary>Configures the database mapping for <see cref="ImageContent"/>.</summary>
public sealed class ImageContentConfiguration : IEntityTypeConfiguration<ImageContent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ImageContent> builder)
    {
        builder.ToTable("ImageContents");

        builder.HasKey(content => content.ImageId);

        builder.Property(content => content.ImageId)
            .ValueGeneratedNever();

        builder.Property(content => content.Data)
            .HasColumnType("mediumblob")
            .IsRequired();

        // No navigation on Image: the data is only ever loaded on its own, never with an Include.
        builder.HasOne<Image>()
            .WithOne()
            .HasForeignKey<ImageContent>(content => content.ImageId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
