#if NETSTANDARD2_0 || NETSTANDARD2_1
namespace System.Runtime.CompilerServices;

using System.ComponentModel;

[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit { }
#endif
