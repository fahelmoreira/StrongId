namespace StrongId.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class StrongIdPrefixAttribute(string prefix) : Attribute
{
    public string Prefix { get; } = prefix;
}