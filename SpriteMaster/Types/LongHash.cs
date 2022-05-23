using SpriteMaster.Hashing;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types;

internal static class LongHash {
	internal const ulong Null = HashUtility.Null;

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal static ulong GetLongHashCode<T>(this T obj) {
		if (obj is ILongHash hashable) {
			return hashable.GetLongHashCode();
		}
		return From(obj);
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal static ulong From(int hashCode) => HashUtility.Combine((ulong)hashCode, (ulong)(~hashCode) << 32);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal static ulong From(ILongHash obj) => obj.GetLongHashCode();

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal static ulong From<T>(T obj) {
		if (obj is ILongHash hashable) {
			return hashable.GetLongHashCode();
		}
		if (obj is null) {
			return Null;
		}
		return From(obj.GetHashCode());
	}
}

internal interface ILongHash {
	ulong GetLongHashCode();
}
