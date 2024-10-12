using System.ComponentModel.DataAnnotations;
using MyBGList.Attributes;

namespace MyBGList.DTO;

public class DomainDTO : IValidatableObject
{
    [Required]
    public int Id { get; set; }

    [LettersOnly]
    public string? Name { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validator = new NameValidatorAttribute();
        if (Id != 3 && Name != "Wargames")
            return [new ValidationResult("Id and/or Name values must\nmatch an allowed Domain.")];
        return [];
    }
}