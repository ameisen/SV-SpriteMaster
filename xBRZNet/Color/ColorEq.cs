using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.xBRZ.Color {
	internal sealed class ColorEq : ColorDist {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ColorEq (in Config configuration) : base(in configuration) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsColorEqual (int color1, int color2) {
			var eqColorThres = Configuration.EqualColorTolerancePow2;
			return DistYCbCr(color1, color2) < eqColorThres;
		}
	}
}
