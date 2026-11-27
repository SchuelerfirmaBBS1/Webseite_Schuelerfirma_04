using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Schuelerfirma.Api.Infrastructure.Persistence;

/// <summary>
/// Ensures that all <see cref="DateTime"/> values read from MySQL are marked as <see cref="DateTimeKind.Utc"/>.
/// </summary>
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}
