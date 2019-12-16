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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public double DistYCbCr(in int pix1, in int pix2)
		{
			if (pix1 == pix2) return 0;

			//http://en.wikipedia.org/wiki/YCbCr#ITU-R_BT.601_conversion
			//YCbCr conversion is a matrix multiplication => take advantage of linearity by subtracting first!
			int rDiff = (int)(((pix1 & ColorConstant.Mask.Red) - (pix2 & ColorConstant.Mask.Red)) >> ColorConstant.Shift.Red); //we may delay division by 255 to after matrix multiplication
			int gDiff = (int)(((pix1 & ColorConstant.Mask.Green) - (pix2 & ColorConstant.Mask.Green)) >> ColorConstant.Shift.Green);
			int bDiff = (int)(((pix1 & ColorConstant.Mask.Blue) - (pix2 & ColorConstant.Mask.Blue)) >> ColorConstant.Shift.Blue); //subtraction for int is noticeable faster than for double

			const double kB = 0.0722; //ITU-R BT.709 conversion
			const double kR = 0.2126;
			const double kG = 1 - kB - kR;

			const double scaleB = 0.5 / (1 - kB);
			const double scaleR = 0.5 / (1 - kR);

			var y = kR * rDiff + kG * gDiff + kB * bDiff; //[!], analog YCbCr!
			var cB = scaleB * (bDiff - y);
			var cR = scaleR * (rDiff - y);

			// Skip division by 255.
			// Also skip square root here by pre-squaring the config option equalColorTolerance.
			return Math.Pow(Configuration.LuminanceWeight * y, 2) + Math.Pow(cB, 2) + Math.Pow(cR, 2);
		}
	}
}
