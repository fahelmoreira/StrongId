using StrongId.Attributes;
using StrongId.Configuration;

namespace Demo;

[StrongIdPrefix("cart", IdType.SequenceString)] public partial class ShoppingCartId;

[StrongIdPrefix("list")] public partial class ShoppingListId;
