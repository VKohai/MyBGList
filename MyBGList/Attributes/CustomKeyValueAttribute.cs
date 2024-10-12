namespace MyBGList.Attributes;

[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Parameter,
    AllowMultiple = true)]
public class CustomKeyValueAttribute : Attribute
{
    public string? Key { get; set; }
    public string? Value { get; set; }

    public CustomKeyValueAttribute(string? key, string? value)
    {
        Key = key;
        Value = value;
    }
}