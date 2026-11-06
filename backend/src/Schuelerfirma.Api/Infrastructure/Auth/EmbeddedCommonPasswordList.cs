using System.Reflection;
using Schuelerfirma.Api.Application.Auth;

namespace Schuelerfirma.Api.Infrastructure.Auth;

/// <summary>
/// The list of common passwords that is embedded into the assembly (<c>Resources/common-passwords.txt</c>),
/// so checking a password needs no network access.
/// </summary>
public sealed class EmbeddedCommonPasswordList : ICommonPasswordList
{
    private const string ResourceName = "common-passwords.txt";

    private static readonly Lazy<HashSet<string>> Passwords = new(Load);

    /// <summary>Number of passwords on the list.</summary>
    public int Count => Passwords.Value.Count;

    /// <inheritdoc />
    public bool Contains(string password) => Passwords.Value.Contains(password);

    private static HashSet<string> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The embedded resource '{ResourceName}' is missing.");
        using var reader = new StreamReader(stream);

        HashSet<string> passwords = new(StringComparer.OrdinalIgnoreCase);
        while (reader.ReadLine() is { } line)
        {
            if (line.Length > 0)
            {
                passwords.Add(line);
            }
        }

        return passwords;
    }
}
