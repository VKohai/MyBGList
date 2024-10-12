using System.ComponentModel.DataAnnotations;

namespace MyBGList.Attributes;

public class NameValidatorAttribute()
    : ValidationAttribute("Value must contain only letters (no spaces, digits, or other chars)")
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var name = value as string;
        if (!string.IsNullOrEmpty(name) && name.All(char.IsLetter))
            return ValidationResult.Success;
        return new ValidationResult(ErrorMessage);
    }
}