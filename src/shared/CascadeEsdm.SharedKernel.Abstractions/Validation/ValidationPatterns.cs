using System.Text.RegularExpressions;

namespace CascadeEsdm.SharedKernel.Validation;

public static class ValidationPatterns
{
    public const string GuidPattern = @"(?im)[{(]?[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?";
    public const string Base64 = @"^[-A-Za-z0-9]+==?$";
    public static bool IsGuid(string value)
    {
        return Regex.IsMatch(value, GuidPattern);
    }
}