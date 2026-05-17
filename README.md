
![StrongId Logo](https://github.com/fahelmoreira/StrongId/blob/main/Assets/stronId-logo.png?raw=true)


# StrongId

Strongly-typed, prefixed identifiers for .NET. A `ShoppingCartId` is not assignable to a `ShoppingListId`, even though both wrap a string. Identifiers are `prefix_<value>` so they sort chronologically, are URL-safe, and self-describe their type at a glance.

Pick the generation scheme that fits the column:
- `Uuid7` — chronologically sortable UUID v7 (32 hex chars). The default.
- `Uuid4` — random UUID v4 (32 hex chars).
- `SequenceString` — **compact 18-char Crockford-base32** id, 48-bit ms timestamp + 40-bit randomness. ~44% shorter than a UUID, still time-sortable for index locality.

Example values:
```
cart_06f2d9s7dndqr9jdhw                     ← SequenceString
prod_019e26a7276c7ec6b7786c9335345133       ← Uuid7
cust_03179215585947b89d823dd774160225       ← Uuid4
```

## Solution layout

| Project | Purpose |
| --- | --- |
| `StrongId` | Core types and the source generator. Multi-targeted: `net8.0`, `net9.0`, `net10.0` and `netstandard2.1` for the library; `netstandard2.0` for the analyzer. |
| `StrongId.EntityFramework` | EF Core `ValueConverter`s and `PropertyBuilder` extensions. Multi-targeted `net7.0`, `net8.0`, `net9.0`, `net10.0` — the EF Core major lines up with the runtime. |
| `StrongId.Tests` | xUnit unit tests for the core. |
| `StrongId.EntityFramework.Tests` | xUnit integration tests against an in-memory SQLite database. |
| `Demo` | Console project demonstrating every id scheme and storage format end-to-end. |

## Defining an id

Two requirements:
1. Mark the class `partial`.
2. Apply `[StrongIdPrefix("...")]`.

```csharp
using StrongId.Attributes;
using StrongId.Configuration;

// Uses the global default scheme (Uuid7 unless overridden).
[StrongIdPrefix("list")]
public partial class ShoppingListId;

// Compact 18-char time-sortable id.
[StrongIdPrefix("cart", IdScheme.SequenceString)]
public partial class ShoppingCartId;

// UUID v7 generation but forced to string storage in EF
// (keeps the "ord_" prefix in the database column).
[StrongIdPrefix("ord", IdScheme.Uuid7, StorageFormat.String)]
public partial class OrderId;

// SequenceString id with a type-bound salt — the random portion carries a 16-bit
// HMAC signature so a `sess_…` string generated for one type cannot be re-parsed
// as another id type sharing the same prefix. See "Salting SequenceString ids".
[StrongIdPrefix("sess", IdScheme.SequenceString, salt: "session-v1")]
public partial class SessionId;
```

The source generator emits a second partial for each id with:
- the `: StrongIdBase<TSelf>` base class — so the user-written declaration stays a one-liner with no generic self-reference;
- a **private parameterless constructor** — `new ShoppingCartId()` is a compile error externally, forcing all construction through factory methods;
- the `IStrongIdFactory<TSelf>` static-abstract-interface implementation — used internally by `StrongIdBase<T>` to construct instances without needing a `new()` constraint (on `netstandard2.1` the same `public static TSelf NewInstance(string)` method satisfies the contract by convention and is invoked reflectively);
- `[JsonConverter(typeof(StrongIdJsonConverter))]` — `System.Text.Json` serializes ids as flat strings, no `JsonSerializerOptions` setup needed at the call site;
- `[TypeConverter(typeof(IdTypeConverter<TSelf>))]` — `System.ComponentModel.TypeDescriptor` resolves a converter that round-trips the id to and from `string` (and `Guid` for UUID-backed schemes), so ASP.NET Core model binding, `IConfiguration.Get<T>()`, designers and any other reflection-based conversion pipeline pick it up automatically.

The generator runs on `partial` classes carrying `[StrongIdPrefix]`. Classes that already inherit from `StrongIdBase<T>` directly are also picked up for backwards compatibility. Non-partial declarations are ignored.

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

## Global defaults

`StrongIdDefaults` configures the fallback scheme/storage for any `[StrongIdPrefix]` that doesn't specify its own. Attribute values always override the global default.

```csharp
using StrongId.Configuration;

StrongIdDefaults.Configure(c =>
{
    c.IdScheme = IdScheme.SequenceString;     // default for ids without an explicit scheme
    c.StorageFormat = StorageFormat.Native;   // default storage policy for EF
    c.UseSalt = true;                         // sign every SequenceString id with its type's salt
});
```

| Setting | Values | Default |
| --- | --- | --- |
| `IdScheme` | `Uuid7`, `Uuid4`, `SequenceString` | `Uuid7` |
| `StorageFormat` | `Native`, `String` | `Native` |
| `IgnoreSuffixValidation` | `bool` | `false` |
| `UseSalt` | `bool` | `false` |

## Core API

`StrongIdBase<T>` exposes:

| Member | Purpose |
| --- | --- |
| `static T Create()` | Generates a new id using the resolved `IdScheme`. Throws `MissingFieldException` if `[StrongIdPrefix]` is missing. |
| `internal static T Create(DateTimeOffset timestamp)` | `SequenceString`-only — generates an id whose 48-bit time prefix is `timestamp` instead of `UtcNow`. Useful for back-mapping historical ids (e.g. backfilling from a legacy `created_at`). Throws `NotSupportedException` for other schemes. |
| `static T Empty` | Returns `<prefix>_empty` (or `_empty` if no prefix attribute). |
| `static T FromString(string value)` | Parses a string. Throws `InvalidCastException` on wrong prefix or malformed suffix; throws `MissingFieldException` if the type has no prefix attribute. |
| `static bool TryParse(string value, out T result)` | Non-throwing variant. |
| `static string Prefix` | The configured prefix from the attribute. |
| `Validate(...)` | `IValidatableObject` — flags a `Value` whose prefix doesn't match. |
| `Equals` / `==` / `!=` / `GetHashCode` | Value semantics over the underlying string. |
| `ToString()` | Returns the underlying string. |

```csharp
var cartId = ShoppingCartId.Create();           // cart_06f2d9s7dndqr9jdhw

var parsed = ShoppingCartId.FromString("cart_06f2d9s7dndqr9jdhw");

if (ShoppingCartId.TryParse(input, out var id))
{
    // safe path
}
```

## The `SequenceString` scheme

A compact alternative to UUID for primary-key columns:

- **18 characters** (vs 32 for UUID `N` format) — ~44% shorter, halves index size for clustered PKs.
- **Time-sortable**: 48-bit Unix-ms timestamp in the high bits, so lexicographic order matches generation order and inserts stay locality-friendly.
- **Collision-safe**: 40 bits of cryptographic randomness — birthday-collision 50% at ~1M ids generated in the *same millisecond*.
- **Crockford Base32 alphabet** (`0-9 a-z` minus `i l o u`) — case-insensitive, URL-safe, no ambiguous characters.

Encoded layout: `prefix_<10 chars timestamp><8 chars randomness>` (e.g. `cart_06f2d9s7dndqr9jdhw`).

## Salting `SequenceString` ids

By default, any 18-char Crockford-base32 suffix with the right prefix is a structurally valid id — `cart_…` and `order_…` would both accept the same suffix bytes. **Salting** binds the suffix to its id type by replacing the last 2 of the 5 random bytes with a 16-bit `HMAC-SHA256(salt, timestamp ‖ random)` signature. Round-trip parsing recomputes the HMAC and rejects mismatches, so re-prefixing a suffix from one type to another is detected at `FromString`.

Format stays 18 chars; the trade-off is collision space drops from 40 to 24 random bits (~50% birthday collision at ~4 800 ids in the same millisecond — still well beyond realistic per-millisecond throughput).

### Per-type opt-in

Pass a non-null `salt:` to the attribute. The source generator emits the value into the generated `NewInstance` factory, where it populates the `protected string? Salt { get; init; }` property on `StrongIdBase<T>` — same object-initializer pattern as `Value`, but `protected` so it never appears in your public API surface.

```csharp
// Salted with an explicit value.
[StrongIdPrefix("sess", IdScheme.SequenceString, salt: "session-v1")]
public partial class SessionId;

var id = SessionId.Create();                     // sess_06f3b166sp3n44pjnc
SessionId.FromString(id.Value);                  // OK
SessionId.FromString("sess_" + otherSuffix);     // InvalidCastException — bad signature
```

### Global opt-in

Setting `StrongIdDefaults.Options.UseSalt = true` salts **every** `SequenceString` id type at runtime, using `prefix + ClassName` as the default salt for types that don't supply one explicitly. Per-type `salt:` arguments still take precedence:

```csharp
StrongIdDefaults.Configure(o => o.UseSalt = true);

// CartId has no salt: arg → uses "cartShoppingCartId" as the runtime salt.
// SessionId still uses "session-v1" (per-type wins).
```

Toggling `UseSalt` at runtime is honored — the salt is resolved per-call, not cached across the flag.

### Bypassing validation

Existing `StrongIdDefaults.Options.IgnoreSuffixValidation = true` short-circuits both alphabet/length and signature checks. Useful for migrating legacy values or running diagnostics.

## JSON serialization

Because the generator emits `[JsonConverter]` on each id type, serialization is transparent:

```csharp
var cart = new ShoppingCart { Id = ShoppingCartId.Create(), Items = [...] };

var json = JsonSerializer.Serialize(cart);
// {"Id":"cart_06f2d9s7dndqr9jdhw","Items":[...]}

var roundTrip = JsonSerializer.Deserialize<ShoppingCart>(json);
```

A wrong prefix during deserialization throws `InvalidCastException`, so an `order_…` value will never silently land in a `UserId` property.

## `TypeConverter` integration

The generator stamps `[TypeConverter(typeof(IdTypeConverter<TSelf>))]` onto each id, so any code path that goes through `TypeDescriptor.GetConverter(...)` works out of the box — no per-id registration:

```csharp
// ASP.NET Core route/query binding: `GET /users/{id}`
public IActionResult Get(UserId id) => Ok(id);

// IConfiguration → strongly-typed options
public class AppOptions { public UserId AdminId { get; set; } = null!; }
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));
// appsettings.json: { "App": { "AdminId": "user_019df8fe572c79e186922e1a64fd2bbf" } }
```

The converter handles both directions for `string` and `Guid`:

| Source / Target | Behavior |
| --- | --- |
| `string` → `TId` | `StrongIdBase<TId>.FromString(...)` — same validation as JSON deserialization. |
| `Guid` → `TId` | `StrongIdBase<TId>.FromUuid(...)` — useful when persistence stores only the bare UUID. |
| `TId` → `string` | Returns the prefixed `Value`. |
| `TId` → `Guid` | Returns the underlying `Uuid` (UUID-backed schemes only). |

A wrong prefix or malformed value throws `InvalidCastException`, matching `FromString` semantics.

## Entity Framework Core

`StrongId.EntityFramework` ships three `PropertyBuilder<T>` extensions:

```csharp
using StrongId.EntityFramework.Extension;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>(b =>
    {
        b.HasKey(p => p.Id);
        b.Property(p => p.Id).HasAutoConversion();      // picks the converter automatically
    });

    // Explicit overrides remain available:
    modelBuilder.Entity<Customer>(b => b.Property(c => c.Id).HasUuIdConversion());
    modelBuilder.Entity<Tag>(b      => b.Property(t => t.Id).HasStringConversion());
}
```

| Extension | Behavior |
| --- | --- |
| `HasAutoConversion<T>()` | Reads the resolved `IdScheme` / `StorageFormat` for `T` and picks the appropriate converter. **Recommended default.** |
| `HasUuIdConversion<T>()` | Forces UUID/Guid storage — only the hex part is persisted; the prefix is reconstructed on read. |
| `HasStringConversion<T>()` | Forces full-string storage — the prefixed value is persisted as-is. |

### `HasAutoConversion` selection rules

| `StorageFormat` | `IdScheme` | Converter chosen | Column type |
| --- | --- | --- | --- |
| `String` | *(any)* | `ConvertToString<T>` | `TEXT` — full prefixed string |
| `Native` | `Uuid7` / `Uuid4` | `ConvertToUUId<T>` | `Guid` / `uuid` (Postgres) / `TEXT` (SQLite) — bare GUID |
| `Native` | `SequenceString` | `ConvertToString<T>` | `TEXT` — full prefixed string (SequenceString isn't a Guid) |

All three extensions constrain `T : StrongIdBase<T>, IStrongIdFactory<T>` so misuse is a compile error.

## Why `partial`?

`System.Text.Json` does not honor `[JsonConverter]` declared on a base class for derived types (it reads the attribute with `inherit: false`); the same is true for `[TypeConverter]`. The library uses a Roslyn source generator to put both attributes (and the private constructor and the factory implementation) directly on each derived id class, which requires the user-written declaration to be `partial`. This is the single boilerplate cost: one keyword.

## Building and testing

```bash
# Build the solution
dotnet build StrongId.slnx

# Unit tests
dotnet test StrongId.Tests/StrongId.Tests.csproj

# EF integration tests (uses an in-memory SQLite database; no Docker required)
dotnet test StrongId.EntityFramework.Tests/StrongId.EntityFramework.Tests.csproj

# Run the demo — generates every id scheme, round-trips through JSON and SQLite
dotnet run --project Demo/Demo.csproj
```

## License

MIT — see [LICENSE](LICENSE).
