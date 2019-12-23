using Microsoft.Xna.Framework;
using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions {
	internal static class Floating {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int NearestInt (this float v) {
			return (int)Math.Round(v);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int NearestInt (this double v) {
			return (int)Math.Round(v);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int NextInt (this float v) {
			return (int)Math.Ceiling(v);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int NextInt (this double v) {
			return (int)Math.Ceiling(v);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int TruncateInt (this float v) {
			return (int)v;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int TruncateInt (this double v) {
			return (int)v;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int ToCoordinate (this float coordinate) {
			return coordinate.NearestInt();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static int ToCoordinate (this double coordinate) {
			return coordinate.NearestInt();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static float Saturate (this float v) {
			return v.Clamp(0.0f, 1.0f);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static double Saturate (this double v) {
			return v.Clamp(0.0, 1.0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static Vector2I ToCoordinate (this Vector2 coordinate) {
			return new Vector2I(coordinate.X.NearestInt(), coordinate.Y.NearestInt());
		}
	}
}
