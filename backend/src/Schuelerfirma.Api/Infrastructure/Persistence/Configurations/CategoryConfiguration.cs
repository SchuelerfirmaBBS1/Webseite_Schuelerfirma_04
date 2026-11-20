using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence.Configurations;

/// <summary>Configures the database mapping for <see cref="Category"/>.</summary>
public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(category => category.CategoryId);

        builder.Property(category => category.CategoryId)
            .ValueGeneratedOnAdd();

        builder.Property(category => category.Name)
            .HasMaxLength(Category.NameMaxLength)
            .UseCollation(Collations.CaseInsensitive)
            .IsRequired();

        builder.Property(category => category.IsVisible)
            .IsRequired();

        builder.HasIndex(category => category.Name)
            .IsUnique()
            .HasDatabaseName("UX_Categories_Name");
    }
}
