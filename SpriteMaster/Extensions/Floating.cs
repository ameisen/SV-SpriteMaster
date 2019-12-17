using System;

namespace SpriteMaster.Extensions {
	internal static class Floating {
		internal static int NearestInt (this float v) {
			return (int)Math.Round(v);
		}
		internal static int NearestInt (this double v) {
			return (int)Math.Round(v);
		}
		internal static int NextInt (this float v) {
			return (int)Math.Ceiling(v);
		}
		internal static int NextInt (this double v) {
			return (int)Math.Ceiling(v);
		}
		internal static int TruncateInt (this float v) {
			return (int)v;
		}
		internal static int TruncateInt (this double v) {
			return (int)v;
		}
	}
}
