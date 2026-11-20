namespace Schuelerfirma.Api.Application.Auth;

/// <summary>
/// Makes text that comes from a client safe to write into a log line.
/// </summary>
public static class LogText
{
    private const int MaxLength = 64;

    /// <summary>Replaces control characters (line breaks!) and shortens the text, so a user name cannot forge log entries.</summary>
    public static string Safe(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var shortened = value.Length > MaxLength ? value[..MaxLength] + "…" : value;
        return string.Create(shortened.Length, shortened, static (span, source) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = char.IsControl(source[i]) ? '?' : source[i];
            }
        });
    }
}
