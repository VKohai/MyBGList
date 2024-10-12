using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MyBGList.Attributes;

public class LettersOnlyAttribute()
    : ValidationAttribute("Value must contain only letters (no spaces, digits, or other chars)")
{
    public bool UseRegex { get; set; } = false;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var str = value as string;
        if (string.IsNullOrEmpty(str))
            return new ValidationResult(ErrorMessage);

        var isValid = UseRegex ? Regex.IsMatch(str, @"^[\p{L}]+$") : str.All(char.IsLetter);
        return isValid ? ValidationResult.Success : new ValidationResult(ErrorMessage);
    }
}