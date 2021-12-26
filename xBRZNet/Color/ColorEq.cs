using System.Runtime.CompilerServices;

namespace SpriteMaster.xBRZ.Color;

sealed class ColorEq : ColorDist {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal ColorEq (Config configuration) : base(configuration) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal bool IsColorEqual (uint color1, uint color2) {
		var equalColorThreshold = Configuration.EqualColorToleranceSq;
		return DistYCbCr(color1, color2) < equalColorThreshold;
	}
}
