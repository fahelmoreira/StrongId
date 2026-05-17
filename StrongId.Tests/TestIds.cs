using StrongId.Attributes;
using StrongId.Base;
using StrongId.Configuration;

namespace StrongId.Tests;

[StrongIdPrefix("user")]
public partial class UserId;

[StrongIdPrefix("order")]
public partial class OrderId;

[StrongIdPrefix("cart", IdScheme.SequenceString)]
public partial class CartId;

// Per-type opt-in via a non-null `salt:` arg. The value populates the protected
// Salt property on StrongIdBase through the source-generated NewInstance factory.
[StrongIdPrefix("salted", IdScheme.SequenceString, salt: "salted-salt-value")]
public partial class SaltedId;

// Custom salt override.
[StrongIdPrefix("custom", IdScheme.SequenceString, salt: "my-custom-salt")]
public partial class CustomSaltId;

// Plain SequenceString id used to exercise the global `StrongIdOptions.UseSalt`
// fallback (no per-type opt-in on the attribute).
[StrongIdPrefix("global", IdScheme.SequenceString)]
public partial class GloballySaltableId;

// Two salted ids sharing the same prefix but with different salts — the cross-type
// forgery test relies on the salt difference (not the prefix or classname).
[StrongIdPrefix("share", IdScheme.SequenceString, salt: "share-A-salt")]
public partial class ShareTypeAId;

[StrongIdPrefix("share", IdScheme.SequenceString, salt: "share-B-salt")]
public partial class ShareTypeBId;

public partial class NoPrefixId : StrongIdBase<NoPrefixId>;
