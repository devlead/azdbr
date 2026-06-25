using System.Text.RegularExpressions;

namespace AZDBR.Services;

/// <summary>
/// Validates and formats SQL Server identifiers.
/// </summary>
public static partial class SqlIdentifierValidator
{
    private const int MaxIdentifierLength = 128;

    [GeneratedRegex(@"^[-a-zA-Z0-9_@#$]+$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();

    /// <summary>
    /// Validates a SQL identifier and returns a bracket-quoted name.
    /// </summary>
    /// <param name="identifier">The identifier to validate.</param>
    /// <returns>A bracket-quoted SQL identifier.</returns>
    /// <exception cref="ArgumentException">Thrown when the identifier is invalid.</exception>
    public static string ToBracketedIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        var trimmed = identifier.Trim('[', ']');
        if (trimmed.Length > MaxIdentifierLength)
        {
            throw new ArgumentException($"SQL identifier '{identifier}' exceeds {MaxIdentifierLength} characters.");
        }

        if (!IdentifierRegex().IsMatch(trimmed))
        {
            throw new ArgumentException($"Invalid SQL identifier '{identifier}'.");
        }

        return $"[{trimmed.Replace("]", "]]", StringComparison.Ordinal)}]";
    }
}
