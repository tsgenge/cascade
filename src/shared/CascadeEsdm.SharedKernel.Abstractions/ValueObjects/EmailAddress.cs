using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CascadeEsdm.SharedKernel.ValueObjects;

public record EmailAddress : IValueObject<string>
{
    // ReSharper disable once MemberCanBePrivate.Global
    public const string Pattern = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$";

    public EmailAddress(string value)
    {
        if (!Regex.IsMatch(value, Pattern))
            throw new ValidationException("Invalid email address");

        Value = value;
    }

    public string Value { get; }
}