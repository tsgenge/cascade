using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CascadeEsdm.SharedKernel.ValueObjects;

public record ClientChannel : IValueObject<string>
{
    private const string Pattern = @"^[0-9a-zA-Z-_]{15,32}$";
    private const string DuffPattern = @"^(n\/a)|((0){32}\/(0){32})$";

    public ClientChannel(string value)
    {
        if (!Regex.IsMatch(value, DuffPattern)) {
            if (!Regex.IsMatch(value, Pattern))
                throw new ValidationException($"[{value}] is not a valid client channel Id.");

            Value = value;
        }
        else {
            Value = "n/a";
        }
    }

    public bool Valid => Value != "n/a";

    // ReSharper disable once MemberCanBePrivate.Global
    public static ClientChannel Empty => new("n/a");

    public string Value { get; }

    public static ClientChannel ParseFromHeader(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return Empty;

        return new ClientChannel(header!);
    }
}