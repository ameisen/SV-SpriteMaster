using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SpriteMaster.Extensions {
	internal static class Arrays {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Span<U> CastAs<T, U>(this T[] data) where T : struct where U : struct {
			return MemoryMarshal.Cast<T, U>(data.AsSpan());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Span<U> CastAs<T, U> (this Span<T> data) where T : struct where U : struct {
			return MemoryMarshal.Cast<T, U>(data);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static ReadOnlySpan<U> CastAs<T, U> (this ReadOnlySpan<T> data) where T : struct where U : struct {
			return MemoryMarshal.Cast<T, U>(data);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static T[] Reverse<T> (this T[] array) {
			Contract.AssertNotNull(array);
			Array.Reverse(array);
			return array;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static T[] Reversed<T> (this T[] array) {
			Contract.AssertNotNull(array);
			var result = (T[])array.Clone();
			Array.Reverse(result);
			return result;
		}
	}
}
