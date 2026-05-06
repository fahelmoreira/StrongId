namespace StrongId.Interfaces;

public interface IStrongId : IConvertible
{
    internal string Value { get; init; }
}