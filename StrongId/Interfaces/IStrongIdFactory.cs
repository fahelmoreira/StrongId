namespace StrongId.Interfaces;

public interface IStrongIdFactory<TSelf> where TSelf : IStrongIdFactory<TSelf>
{
    static abstract TSelf NewInstance(string value);
}
