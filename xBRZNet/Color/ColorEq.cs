using System;
using System.Runtime.CompilerServices;

namespace xBRZNet2.Color
{
	internal sealed class ColorEq : ColorDist
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ColorEq(in Config configuration) : base(configuration) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsColorEqual(in int color1, in int color2)
		{
			var eqColorThres = Configuration.EqualColorTolerancePow2;
			return DistYCbCr(color1, color2) < eqColorThres;
		}
	}
}
