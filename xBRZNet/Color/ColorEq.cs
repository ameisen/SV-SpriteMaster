using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.xBRZ.Color {
	internal sealed class ColorEq : ColorDist {
		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal ColorEq (in Config configuration) : base(in configuration) { }

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal bool IsColorEqual (uint color1, uint color2) {
			var eqColorThres = Configuration.EqualColorToleranceSq;
			return DistYCbCr(color1, color2) < eqColorThres;
		}
	}
}
