using System.Runtime.CompilerServices;
using xBRZNet2.Common;

namespace xBRZNet2.Blend {
	internal static class BlendInfo {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static char GetTopL (this char b) { return unchecked((char)(b & 0x3)); }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static char GetTopR (this char b) { return unchecked((char)((b >> 2) & 0x3)); }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static char GetBottomR (this char b) { return unchecked((char)((b >> 4) & 0x3)); }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static char GetBottomL (this char b) { return unchecked((char)((b >> 6) & 0x3)); }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static char SetTopL (this char b, char bt) { return unchecked((char)(b | bt)); }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static char SetTopR (this char b, char bt) { return unchecked((char)(b | (bt << 2))); }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static char SetBottomR (this char b, char bt) { return unchecked((char)(b | (bt << 4))); }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static char SetBottomL (this char b, char bt) { return unchecked((char)(b | (bt << 6))); }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static char Rotate (this char b, RotationDegree rotDeg) {
			var l = (int)rotDeg << 1;
			var r = 8 - l;

			return unchecked((char)(b << l | b >> r));
		}
	}
}
