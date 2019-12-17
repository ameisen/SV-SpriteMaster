using SpriteMaster.Types;
using System;

namespace SpriteMaster.Extensions {
	internal static class Common {
		internal static void ConditionalSet<T> (this ref T obj, bool conditional, in T value) where T : struct {
			if (conditional) {
				obj = value;
			}
		}

		internal static int ClampDimension (this int value) {
			return Math.Min(value, Config.ClampDimension);
		}

		internal static Vector2I ClampDimension (this Vector2I value) {
			return value.Min(Config.ClampDimension);
		}

		internal static void Swap<T> (ref T l, ref T r) {
			(l, r) = (r, l);
		}
	}
}
