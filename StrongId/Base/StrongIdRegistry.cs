using System.Reflection;
using StrongId.Attributes;
using StrongId.Interfaces;

namespace StrongId.Base;

/// <summary>
/// Lazily-built, cached map from prefix to the concrete <see cref="StrongIdBase{T}"/> type
/// that declares it. Assemblies are scanned at most once; only assemblies that reference
/// StrongId are inspected, and newly loaded assemblies are picked up on a lookup miss.
/// </summary>
internal static class StrongIdRegistry
{
    private sealed class Entry(Type type, Func<string, IStrongId> parse)
    {
        public Type Type { get; } = type;
        public Func<string, IStrongId> Parse { get; } = parse;
    }

    private static readonly object Gate = new();
    private static readonly Assembly CoreAssembly = typeof(IStrongId).Assembly;
    private static readonly string CoreAssemblyName = CoreAssembly.GetName().Name!;
    private static readonly HashSet<Assembly> Scanned = [];
    private static readonly Dictionary<string, Entry> ByPrefix = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, List<Type>> Ambiguous = new(StringComparer.Ordinal);

    // Bumped whenever an assembly is scanned so lock-free readers see a fresh snapshot.
    private static volatile Dictionary<string, Entry> _snapshot = new(StringComparer.Ordinal);

    public static IStrongId Parse(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        // Suffixes never contain '_', so the prefix is everything before the last '_'
        // (keeps prefixes that themselves contain '_' resolvable).
        var prefix = StrongId.ExtractPrefix(value);

        if (!_snapshot.TryGetValue(prefix, out var entry))
        {
            entry = Resolve(prefix);
        }

        return entry.Parse(value);
    }

    private static Entry Resolve(string prefix)
    {
        lock (Gate)
        {
            ScanNewAssemblies();

            if (Ambiguous.TryGetValue(prefix, out var candidates))
            {
                throw new InvalidOperationException(
                    $"The prefix '{prefix}' is declared by multiple id types ({string.Join(", ", candidates.Select(t => t.FullName))}); " +
                    "use the concrete type's FromString instead.");
            }

            if (ByPrefix.TryGetValue(prefix, out var entry))
            {
                return entry;
            }
        }

        throw new InvalidCastException($"No StrongId type is registered for prefix '{(string.IsNullOrEmpty(prefix) ? "empty" : prefix)}'.");
    }

    private static void ScanNewAssemblies()
    {
        var changed = false;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!Scanned.Add(assembly) || !ReferencesCore(assembly))
            {
                continue;
            }

            foreach (var type in GetLoadableTypes(assembly))
            {
                if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters || !typeof(IStrongId).IsAssignableFrom(type))
                {
                    continue;
                }

                var attribute = type.GetCustomAttribute<StrongIdPrefixAttribute>(inherit: false);
                var parse = attribute is null ? null : CreateParser(type);

                if (parse is null)
                {
                    continue;
                }

                Register(attribute!.Prefix, new Entry(type, parse));
                changed = true;
            }
        }

        if (changed)
        {
            _snapshot = new Dictionary<string, Entry>(ByPrefix, StringComparer.Ordinal);
        }
    }

    private static void Register(string prefix, Entry entry)
    {
        if (Ambiguous.TryGetValue(prefix, out var list))
        {
            list.Add(entry.Type);
            return;
        }

        if (ByPrefix.TryGetValue(prefix, out var existing))
        {
            if (existing.Type == entry.Type)
            {
                return;
            }

            ByPrefix.Remove(prefix);
            Ambiguous[prefix] = [existing.Type, entry.Type];
            return;
        }

        ByPrefix[prefix] = entry;
    }

    /// <summary>
    /// Binds to the public static <c>FromString(string)</c> inherited from
    /// <see cref="StrongIdBase{T}"/>, so conversion goes through the same prefix/suffix
    /// validation as calling <c>TId.FromString</c> directly.
    /// </summary>
    private static Func<string, IStrongId>? CreateParser(Type type)
    {
        var method = type.GetMethod(
            "FromString",
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
            binder: null,
            types: [typeof(string)],
            modifiers: null);

        if (method is null || !typeof(IStrongId).IsAssignableFrom(method.ReturnType) || method.ReturnType.IsValueType)
        {
            return null;
        }

        return (Func<string, IStrongId>)method.CreateDelegate(typeof(Func<string, IStrongId>));
    }

    private static bool ReferencesCore(Assembly assembly)
    {
        if (assembly == CoreAssembly)
        {
            return true;
        }

        try
        {
            return assembly.GetReferencedAssemblies().Any(a => a.Name == CoreAssemblyName);
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
        catch
        {
            return [];
        }
    }
}
