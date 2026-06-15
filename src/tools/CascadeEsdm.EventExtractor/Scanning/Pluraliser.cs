namespace CascadeEsdm.EventExtractor.Scanning;

/// <summary>
/// Simple English pluraliser for aggregate names.
/// Handles common English pluralisation rules without external dependencies.
/// </summary>
internal static class Pluraliser
{
    private static readonly Dictionary<string, string> Irregulars = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Person"] = "People",
        ["Child"] = "Children",
        ["Man"] = "Men",
        ["Woman"] = "Women",
        ["Mouse"] = "Mouse",
        ["Goose"] = "Geese",
        ["Tooth"] = "Teeth",
        ["Foot"] = "Feet",
        ["Ox"] = "Oxen",
        ["Leaf"] = "Leaves",
        ["Life"] = "Lives",
        ["Knife"] = "Knives",
        ["Wife"] = "Wives",
        ["Shelf"] = "Shelves",
        ["Self"] = "Selves",
    };

    public static string Pluralise(string singular)
    {
        if (string.IsNullOrEmpty(singular))
            return singular;

        // Check irregulars (case-preserving)
        if (Irregulars.TryGetValue(singular, out var irregular))
            return PreserveCase(singular, irregular);

        // Already plural-looking (ends with 's' but not 'ss', 'us', 'is')
        if (singular.EndsWith("ses", StringComparison.Ordinal) ||
            singular.EndsWith("xes", StringComparison.Ordinal) ||
            singular.EndsWith("zes", StringComparison.Ordinal))
            return singular;

        // Words ending in 'z' after a short vowel → double z + 'es' (quiz → quizzes)
        if (singular.EndsWith('z') && singular.Length > 1 && IsVowel(singular[^2]))
            return singular + "zes";

        // Words ending in 's', 'x', 'z', 'sh', 'ch' → add 'es'
        if (singular.EndsWith('s') || singular.EndsWith('x') || singular.EndsWith('z') ||
            singular.EndsWith("sh", StringComparison.Ordinal) ||
            singular.EndsWith("ch", StringComparison.Ordinal))
            return singular + "es";

        // Words ending in consonant + 'y' → replace 'y' with 'ies'
        if (singular.EndsWith('y') && singular.Length > 1 && IsConsonant(singular[^2]))
            return singular[..^1] + "ies";

        // Words ending in 'f' or 'fe' → 'ves' (common cases handled by irregulars above)
        // Default rule: just add 's'
        return singular + "s";
    }

    private static bool IsConsonant(char c) =>
        char.IsLetter(c) && !"aeiouAEIOU".Contains(c);

    private static bool IsVowel(char c) =>
        "aeiouAEIOU".Contains(c);

    private static string PreserveCase(string original, string replacement)
    {
        if (char.IsUpper(original[0]) && char.IsUpper(replacement[0]))
            return replacement;
        if (char.IsUpper(original[0]))
            return char.ToUpperInvariant(replacement[0]) + replacement[1..];
        return char.ToLowerInvariant(replacement[0]) + replacement[1..];
    }
}
