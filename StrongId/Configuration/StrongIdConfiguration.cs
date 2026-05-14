namespace StrongId.Configuration;

public static class StrongIdConfiguration
{
    private static readonly IdType DefaultIdType = IdType.Uuid7;
    internal static StrongIdOptions ConfigureOptions { get; } = new()
    {
        IdType = DefaultIdType
    };
    
    public static void Configure(Action<StrongIdOptions> options)
    {
        options.Invoke(ConfigureOptions);
        ConfigureOptions.IdType = ConfigureOptions.IdType switch
        {
            IdType.Default => DefaultIdType,
            _ => ConfigureOptions.IdType
        };
    }
}

public enum IdType
{
    Default,
    Uuid7,
    Uuid4,
    Int,
    SequenceString
}

public class StrongIdOptions
{
    public IdType IdType { get; set; } = IdType.Uuid7;
}