using SpriteMaster.Types;
using System.Runtime.CompilerServices;

namespace SpriteMaster.xBRZ.Color;

sealed class ColorEq : ColorDist {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal ColorEq (Config configuration) : base(configuration) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal bool IsColorEqual (Color8 color1, Color8 color2) => DistYCbCr(color1, color2) < Configuration.EqualColorToleranceSq;
}
