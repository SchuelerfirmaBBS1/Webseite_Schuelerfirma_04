using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence.Configurations;

/// <summary>Configures the database mapping for <see cref="User"/>.</summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.UserId);

        builder.Property(user => user.UserId)
            .ValueGeneratedOnAdd();

        builder.Property(user => user.Username)
            .HasMaxLength(User.UsernameMaxLength)
            .UseCollation(Collations.CaseInsensitive)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(User.EmailMaxLength)
            .UseCollation(Collations.CaseInsensitive)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(User.PasswordHashMaxLength)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .IsRequired();

        builder.Property(user => user.FailedLoginCount)
            .IsRequired();

        builder.Property(user => user.LockoutEnd)
            .HasColumnType("datetime(6)")
            .IsRequired(false);

        builder.Property(user => user.MustChangePassword)
            .IsRequired();

        builder.Property(user => user.CreatedAt)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(user => user.LastLoginAt)
            .HasColumnType("datetime(6)")
            .IsRequired(false);

        builder.HasIndex(user => user.Username)
            .IsUnique()
            .HasDatabaseName("UX_Users_Username");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("UX_Users_Email");

        builder.HasIndex(user => user.RoleId)
            .HasDatabaseName("IX_Users_RoleId");

        builder.HasOne(user => user.Role)
            .WithMany(role => role.Users)
            .HasForeignKey(user => user.RoleId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}