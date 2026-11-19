using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence.Configurations;

/// <summary>Configures the database mapping for <see cref="Role"/>.</summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(role => role.RoleId);

        builder.Property(role => role.RoleId)
            .ValueGeneratedOnAdd();

        builder.Property(role => role.Name)
            .HasMaxLength(Role.NameMaxLength)
            .UseCollation(Collations.CaseInsensitive)
            .IsRequired();

        builder.HasIndex(role => role.Name)
            .IsUnique()
            .HasDatabaseName("UX_Roles_Name");

        builder.HasData(DefaultRoles.CreateSeed().Select(r => new { r.RoleId, r.Name }));
    }
}