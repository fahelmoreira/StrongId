# StrongId

Strongly-typed, prefixed identifiers for .NET. A `ShoppingCartId` is not assignable to a `ShoppingListId`, even though both wrap a string. Identifiers are `prefix_<UUID v7 hex>` (e.g. `cart_019df93d327f710db5ff492547609240`) so they sort chronologically, are URL-safe, and self-describe their type at a glance.

## Solution layout

| Project | Purpose |
| --- | --- |
| `StrongId` | Core types and the source generator. Multi-targeted: `net10.0` for the library, `netstandard2.0` for the analyzer. |
| `StrongId.EntityFramework` | EF Core `ValueConverter`s and `PropertyBuilder` extensions. |
| `StrongId.Tests` | xUnit unit tests for the core. |
| `StrongId.EntityFramework.Tests` | xUnit integration tests against an in-memory SQLite database. |
| `Demo` | Minimal console project demonstrating usage. |

## Defining an id

Two requirements:
1. Mark the class `partial`.
2. Apply `[StrongIdPrefix("...")]`.

```csharp
using StrongId.Attributes;
using StrongId.Base;

[StrongIdPrefix("cart")]
public partial class ShoppingCartId : StrongIdBase<ShoppingCartId>;

[StrongIdPrefix("list")]
public partial class ShoppingListId : StrongIdBase<ShoppingListId>;
```

The source generator emits a second partial for each id with:
- a **private parameterless constructor** — `new ShoppingCartId()` is a compile error externally, forcing all construction through factory methods;
- the `IStrongIdFactory<TSelf>` static-abstract-interface implementation — used internally by `StrongIdBase<T>` to construct instances without needing a `new()` constraint;
- `[JsonConverter(typeof(StrongIdJsonConverter))]` — `System.Text.Json` serializes ids as flat strings, no `JsonSerializerOptions` setup needed at the call site.

The generator runs only on `partial` classes that inherit (directly or transitively) from `StrongIdBase<T>`. Non-partial declarations are ignored.

## Wiring the generator into a consumer project

Reference `StrongId` twice — once as a normal library and once as an analyzer pointing at the netstandard2.0 build:

```xml
<ItemGroup>
  <ProjectReference Include="..\StrongId\StrongId.csproj" />
  <ProjectReference Include="..\StrongId\StrongId.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false"
                    SetTargetFramework="TargetFramework=netstandard2.0" />
</ItemGroup>
```

## Core API

`StrongIdBase<T>` exposes:

| Member | Purpose |
| --- | --- |
| `static T Create()` | Generates a new id with a fresh UUID v7. Throws `MissingFieldException` if `[StrongIdPrefix]` is missing. |
| `static T Empty` | Returns `<prefix>_empty` (or `_empty` if no prefix attribute). |
| `static T FromString(string value)` | Parses a string. Throws `InvalidCastException` on wrong prefix or invalid hex; throws `MissingFieldException` if the type has no prefix attribute. |
| `static bool TryParse(string value, out T result)` | Non-throwing variant. |
| `static string Prefix` | The configured prefix from the attribute. |
| `Validate(...)` | `IValidatableObject` — flags a `Value` whose prefix doesn't match. |
| `Equals` / `==` / `!=` / `GetHashCode` | Value semantics over the underlying string. |
| `ToString()` | Returns the underlying string. |

```csharp
var cartId = ShoppingCartId.Create();           // cart_019df93d...

var parsed = ShoppingCartId.FromString("cart_019df8fe572c79e186922e1a64fd2bbf");

if (ShoppingCartId.TryParse(input, out var id))
{
    // safe path
}
```

## JSON serialization

Because the generator emits `[JsonConverter]` on each id type, serialization is transparent:

```csharp
var cart = new ShoppingCart { Id = ShoppingCartId.Create(), Items = [...] };

var json = JsonSerializer.Serialize(cart);
// {"Id":"cart_019df93d...","Items":[...]}

var roundTrip = JsonSerializer.Deserialize<ShoppingCart>(json);
```

A wrong prefix during deserialization throws `InvalidCastException`, so an `order_…` value will never silently land in a `UserId` property.

## Entity Framework Core

`StrongId.EntityFramework` provides two converters and matching extension methods on `PropertyBuilder<T>`:

```csharp
using StrongId.EntityFramework.Extension;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>(b =>
    {
        b.HasKey(p => p.Id);
        b.Property(p => p.Id).HasUuIdConversion();          // stored as UUID/Guid
        b.Property(p => p.OwnerId!).HasUuIdConversion();    // nullable id
    });

    modelBuilder.Entity<Tag>(b =>
    {
        b.HasKey(t => t.Id);
        b.Property(t => t.Id).HasStringConversion();        // stored as full prefixed string
    });
}
```

| Extension | Underlying converter | Storage |
| --- | --- | --- |
| `HasUuIdConversion<T>()` | `Converter.ConvertToUUId<T>` | `Guid` / `uuid` (Postgres) / `TEXT` (SQLite) — only the hex part is persisted; the prefix is reconstructed on read from the type's `[StrongIdPrefix]`. |
| `HasStringConversion<T>()` | `Converter.ConvertToString<T>` | The full prefixed string is persisted as-is. |

Both extensions constrain `T : StrongIdBase<T>, IStrongIdFactory<T>` so misuse is a compile error.

## Why `partial`?

`System.Text.Json` does not honor `[JsonConverter]` declared on a base class for derived types (it reads the attribute with `inherit: false`). The library uses a Roslyn source generator to put the attribute (and the private constructor and the factory implementation) directly on each derived id class, which requires the user-written declaration to be `partial`. This is the single boilerplate cost: one keyword.

## Building and testing

```bash
# Build the solution
dotnet build StrongId.slnx

# Unit tests
dotnet test StrongId.Tests/StrongId.Tests.csproj

# EF integration tests (uses an in-memory SQLite database; no Docker required)
dotnet test StrongId.EntityFramework.Tests/StrongId.EntityFramework.Tests.csproj

# Run the demo
dotnet run --project Demo/Demo.csproj
```

## License

MIT — see [LICENSE](LICENSE).
