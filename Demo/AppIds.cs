using StrongId.Attributes;
using StrongId.Configuration;

namespace Demo;

// No explicit IdType — resolves to the global default configured in Program.cs.
[StrongIdPrefix("list")]
public partial class ShoppingListId;

// Default IdType (Uuid7) + default StoreType (NativeType) — stored as Guid in EF.
[StrongIdPrefix("prod", IdType.Uuid7)]
public partial class ProductId;

// Random UUID (no time component) + native Guid storage.
[StrongIdPrefix("cust", IdType.Uuid4)]
public partial class CustomerId;

// Compact 18-char time-sortable id. Native storage falls back to string
// because SequenceString isn't a Guid.
[StrongIdPrefix("cart", IdType.SequenceString)]
public partial class ShoppingCartId;

// Uuid7 id but forced to string storage — the "ord_" prefix is preserved in the DB column.
[StrongIdPrefix("ord", IdType.Uuid7, StoreType.String)]
public partial class OrderId;
