using SpriteMaster.Extensions;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types;
static class LongHash {
	internal const ulong Null = 0UL;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong GetLongHashCode<T>(this T obj) {
		if (obj is ILongHash hashable) {
			return hashable.GetLongHashCode();
		}
		return From(obj);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong From(int hashCode) => Hash.Combine(hashCode, hashCode << 32);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong From(ILongHash obj) => obj.GetLongHashCode();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong From<T>(T obj) {
		if (obj is ILongHash hashable) {
			return hashable.GetLongHashCode();
		}
		return From(obj.GetHashCode());
	}
}

internal interface ILongHash {
	internal ulong GetLongHashCode();
}
