using Microsoft.Xna.Framework;
using SpriteMaster.Types;
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

		internal static int ToCoordinate (this float coordinate) {
			return coordinate.NearestInt();
		}

		internal static int ToCoordinate (this double coordinate) {
			return coordinate.NearestInt();
		}

		internal static float Saturate (this float v) {
			return v.Clamp(0.0f, 1.0f);
		}

		internal static double Saturate (this double v) {
			return v.Clamp(0.0, 1.0);
		}

		internal static Vector2I ToCoordinate (this Vector2 coordinate) {
			return new Vector2I(coordinate.X.NearestInt(), coordinate.Y.NearestInt());
		}
	}
}
