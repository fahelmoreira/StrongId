using StrongId.Attributes;
using StrongId.Configuration;

namespace Demo;

// No explicit IdScheme — resolves to the global default configured in Program.cs.
[StrongIdPrefix("list")]
public partial class ShoppingListId;

// Default IdScheme (Uuid7) + default StorageFormat (Native) — stored as Guid in EF.
[StrongIdPrefix("prod", IdScheme.Uuid7)]
public partial class ProductId;

// Random UUID (no time component) + native Guid storage.
[StrongIdPrefix("cust", IdScheme.Uuid4)]
public partial class CustomerId;

// Compact 18-char time-sortable id. Native storage falls back to string
// because SequenceString isn't a Guid.
[StrongIdPrefix("cart", IdScheme.SequenceString)]
public partial class ShoppingCartId;

// Uuid7 id but forced to string storage — the "ord_" prefix is preserved in the DB column.
[StrongIdPrefix("ord", IdScheme.Uuid7, StorageFormat.String)]
public partial class OrderId;

// SequenceString id with a salted, type-bound suffix. The suffix carries a 16-bit
// HMAC of `prefix + className`, so a "sess_…" string generated for one type cannot
// be re-parsed as another id type using the same prefix.
[StrongIdPrefix("sess", IdScheme.SequenceString, salt: "string-salt")]
public partial class SessionId;
