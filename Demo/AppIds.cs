using StrongId.Attributes;
using StrongId.Base;

namespace Demo;

[StrongIdPrefix("cart")]
public partial class ShoppingCartId : StrongIdBase<ShoppingCartId>;

[StrongIdPrefix("list")]
public partial class ShoppingListId : StrongIdBase<ShoppingListId>;
