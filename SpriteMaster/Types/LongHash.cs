using SpriteMaster.Extensions;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types {
	public static class LongHash {
		public const ulong Null = 0UL;

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public static ulong GetLongHashCode<T>(this T obj) {
			if (obj is ILongHash hashable) {
				return hashable.GetLongHashCode();
			}
			return From(obj);
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public static ulong From (int hashCode) => Hash.Combine(hashCode, hashCode << 32);

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public static ulong From (ILongHash obj) => obj.GetLongHashCode();

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public static ulong From<T> (T obj) {
			if (obj is ILongHash hashable) {
				return hashable.GetLongHashCode();
			}
			return From(obj.GetHashCode());
		}
	}

	public interface ILongHash {
		public ulong GetLongHashCode ();
	}
}
