namespace StrongId.Attributes;

public class StrongIdPrefixAttribute(string prefix) : Attribute
{
    public string Prefix { get; } = prefix;
}