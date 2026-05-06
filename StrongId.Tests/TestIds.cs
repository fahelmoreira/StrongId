using StrongId.Attributes;
using StrongId.Base;

namespace StrongId.Tests;

[StrongIdPrefix("user")]
public partial class UserId;

[StrongIdPrefix("order")]
public partial class OrderId;

public partial class NoPrefixId : StrongIdBase<NoPrefixId>;
