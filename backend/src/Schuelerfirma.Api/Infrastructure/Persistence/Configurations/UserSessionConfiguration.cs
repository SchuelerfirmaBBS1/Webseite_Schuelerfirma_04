using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Schuelerfirma.Api.Domain;

namespace Schuelerfirma.Api.Infrastructure.Persistence.Configurations;

/// <summary>Configures the database mapping for <see cref="UserSession"/>.</summary>
public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");

        builder.HasKey(session => session.SessionKeyHash);

        builder.Property(session => session.SessionKeyHash)
            .HasColumnType($"char({UserSession.SessionKeyHashLength})")
            .HasCharSet("ascii")
            .UseCollation("ascii_bin")
            .IsUnicode(false)
            .IsFixedLength()
            .IsRequired();

        builder.Property(session => session.Ticket)
            .HasColumnType("LONGBLOB")
            .IsRequired();

        builder.Property(session => session.CreatedAt)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.Property(session => session.ExpiresAt)
            .HasColumnType("datetime(6)")
            .IsRequired();

        builder.HasIndex(session => session.UserId)
            .HasDatabaseName("IX_UserSessions_UserId");

        builder.HasIndex(session => session.ExpiresAt)
            .HasDatabaseName("IX_UserSessions_ExpiresAt");

        builder.HasOne(session => session.User)
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
