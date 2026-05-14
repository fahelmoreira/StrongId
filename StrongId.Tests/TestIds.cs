using StrongId.Attributes;
using StrongId.Base;
using StrongId.Configuration;

namespace StrongId.Tests;

[StrongIdPrefix("user")]
public partial class UserId;

[StrongIdPrefix("order")]
public partial class OrderId;

[StrongIdPrefix("cart", IdType.SequenceString)]
public partial class CartId;

public partial class NoPrefixId : StrongIdBase<NoPrefixId>;
