using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CascadeEsdm.ReadModel.Utilities;

/// <summary>
///     String helpers used when composing view storage keys and translating filters into queries.
/// </summary>
public static class StringExtensions
{
    private static readonly string[] DefaultDelimiters = { ".", "/", "\\" };

    /// <summary>
    ///     Produces a deterministic <see cref="Guid" /> from a string (MD5 of the UTF-8 bytes).
    ///     An empty/whitespace value yields <see cref="Guid.Empty" />.
    /// </summary>
    public static Guid ToGuid(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Guid.Empty;

        using var md5 = MD5.Create();
        return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    /// <summary>
    ///     Converts a delimited identifier to camelCase, stripping whitespace/hyphens/underscores and
    ///     lower-casing the first letter of each segment.
    /// </summary>
    public static string ToCamelCase(this string value, string[]? delimiters = null)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var delims = delimiters ?? DefaultDelimiters;

        var collapsed = Regex.Replace(value, @"(\s|-|_)+", string.Empty);

        if (collapsed.Length == 0)
            return collapsed;

        var result = new StringBuilder(collapsed.Length);
        var wordStart = true;
        foreach (var character in collapsed)
        {
            var c = character.ToString();
            if (!delims.Contains(c))
            {
                if (wordStart)
                    c = c.ToLowerInvariant();
                wordStart = false;
            }
            else
            {
                wordStart = true;
            }

            result.Append(c);
        }

        return result.ToString();
    }
}
