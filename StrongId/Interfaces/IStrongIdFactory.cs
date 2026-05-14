namespace StrongId.Interfaces;

#if NET7_0_OR_GREATER
public interface IStrongIdFactory<TSelf> where TSelf : IStrongIdFactory<TSelf>
{
    static abstract TSelf NewInstance(string value);
}
#else
// On runtimes without static abstract members (e.g. netstandard2.1), the
// contract is satisfied by convention: implementers must expose a
// `public static TSelf NewInstance(string value)` method. The base class
// invokes it reflectively.
public interface IStrongIdFactory<TSelf> where TSelf : IStrongIdFactory<TSelf>
{
}
#endif
