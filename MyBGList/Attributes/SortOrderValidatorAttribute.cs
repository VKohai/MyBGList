using System.ComponentModel.DataAnnotations;

namespace MyBGList.Attributes;

public class SortOrderValidatorAttribute()
    : ValidationAttribute("Value must be one of the following: {0}.")
{
    public string[] AllowedValues { get; set; } = ["ASC", "DESC"];

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var strValue = value as string;
        if (!string.IsNullOrEmpty(strValue) &&
            AllowedValues.Contains(strValue))
            return ValidationResult.Success;
        return new ValidationResult(
            FormatErrorMessage(string.Join(",", AllowedValues))
        );
    }
}