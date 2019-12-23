using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions {
	internal static class Common {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void ConditionalSet<T> (this ref T obj, bool conditional, in T value) where T : struct {
			if (conditional) {
				obj = value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static WeakReference<T> MakeWeak<T>(this T obj) where T : class {
			return new WeakReference<T>(obj);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int ClampDimension (this int value) {
			return Math.Min(value, Config.ClampDimension);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector2I ClampDimension (this Vector2I value) {
			return value.Min(Config.ClampDimension);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void Swap<T> (ref T l, ref T r) {
			(l, r) = (r, l);
		}
	}
}
