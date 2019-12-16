using System;
using System.Runtime.CompilerServices;
using xBRZNet2.Common;

namespace xBRZNet2.Color
{
	internal class ColorDist
	{
		protected readonly Config Configuration;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ColorDist(in Config cfg)
		{
			Configuration = cfg;
		}

		private const bool MultiplyAlpha = false;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int TexelDiff(int texel1, int texel2, in int shift)
		{
			texel1 = unchecked((int)(((uint)texel1 >> shift) & 0xFF));
			texel2 = unchecked((int)(((uint)texel2 >> shift) & 0xFF));

			return Math.Abs(texel1 - texel2);
			//return (unchecked((int)((uint)texel1 & mask)) - unchecked((int)((uint)texel2 & mask))) >> shift;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double DistYCbCr(in int pix1, in int pix2)
		{
			if (pix1 == pix2) return 0;

			//http://en.wikipedia.org/wiki/YCbCr#ITU-R_BT.601_conversion
			//YCbCr conversion is a matrix multiplication => take advantage of linearity by subtracting first!
			var rDiff = TexelDiff(pix1, pix2, ColorConstant.Shift.Red); //we may delay division by 255 to after matrix multiplication
			var gDiff = TexelDiff(pix1, pix2, ColorConstant.Shift.Green);
			var bDiff = TexelDiff(pix1, pix2, ColorConstant.Shift.Blue);  //subtraction for int is noticeable faster than for double

			// Alpha gives some interesting properties.
			// We techncially cannot guarantee that the color is correct once we are in transparent areas, but we might still want to blend there.

			if (MultiplyAlpha)
			{
				var aDiff = 0xFF - TexelDiff(pix1, pix2, ColorConstant.Shift.Alpha);
				rDiff = (rDiff * aDiff) / 0xFF;
				gDiff = (gDiff * aDiff) / 0xFF;
				bDiff = (bDiff * aDiff) / 0xFF;
			}

			const double kB = 0.0722; //ITU-R BT.709 conversion
			const double kR = 0.2126;
			const double kG = 1.0 - kB - kR;

			const double scaleB = 0.5 / (1.0 - kB);
			const double scaleR = 0.5 / (1.0 - kR);

			var y = kR * rDiff + kG * gDiff + kB * bDiff; //[!], analog YCbCr!
			var cB = scaleB * (bDiff - y);
			var cR = scaleR * (rDiff - y);

			// Skip division by 255.
			// Also skip square root here by pre-squaring the config option equalColorTolerance.
			return Math.Pow(Configuration.LuminanceWeight * y, 2) + Math.Pow(cB, 2) + Math.Pow(cR, 2);
		}
	}
}
