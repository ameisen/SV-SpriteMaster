#define XBRZ_WIDE_BLEND
//#define XBRZ_HARDEN_EDGE

using SpriteMaster.Colors;
using SpriteMaster.Types;
using SpriteMaster.Types.Fixed;
using SpriteMaster.xBRZ.Common;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.xBRZ.Scalers;

abstract class IScaler {
	internal readonly int Scale;
	internal readonly Config Configuration;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	protected IScaler(int scale, Config configuration) {
		Scale = scale;
		Configuration = configuration;
	}

	internal abstract void BlendLineSteep(Color8 color, ref OutputMatrix matrix);
	internal abstract void BlendLineSteepAndShallow(Color8 color, ref OutputMatrix matrix);
	internal abstract void BlendLineShallow(Color8 color, ref OutputMatrix matrix);
	internal abstract void BlendLineDiagonal(Color8 color, ref OutputMatrix matrix);
	internal abstract void BlendCorner(Color8 color, ref OutputMatrix matrix);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	protected void AlphaBlend(int n, int m, ref Color8 dstRef, Color8 color) {
		//assert n < 256 : "possible overflow of (color & redMask) * N";
		//assert m < 256 : "possible overflow of (color & redMask) * N + (dst & redMask) * (M - N)";
		//assert 0 < n && n < m : "0 < N && N < M";

		//this works because 8 upper bits are free
		var dst = dstRef;
		var a = BlendComponent(n, m, dst.A, color.A);
		//if (alphaComponent == 0) {
		//	dstRef = 0;
		//	return;
		//}
		var r = BlendComponent(n, m, dst.R, color.R);
		var g = BlendComponent(n, m, dst.G, color.G);
		var b = BlendComponent(n, m, dst.B, color.B);
		dstRef = new(r, g, b, a);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static double Curve(double x) => ((Math.Sin(x * Math.PI - (Math.PI / 2.0))) + 1) / 2;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static Fixed8 BlendComponent(int n, int m, Fixed8 inComponent, Fixed8 setComponent) {
#if XBRZ_WIDE_BLEND
		var blend = setComponent.Value.asSigned() * n + inComponent.Value.asSigned() * (m - n);

		var outChan = (blend / m).asUnsigned() & 0xFFFF;

		// Value is now in the range of 0 to 0xFFFF
#if XBRZ_HARDEN_EDGE
		// If it's alpha, let's try hardening the edges.
		float channelF = (float)outChan / (float)0xFFFF;

			// alternatively, could use sin(x*pi - (pi/2))
			var hardenedAlpha = Curve(channelF);

			outChan = Math.Min(0xFFFF, (uint)(hardenedAlpha * 0xFFFF));
		}
#endif // XBRZ_HARDEN_EDGE

		return (byte)outChan;
#else // XBRZ_WIDE_BLEND
		/*
		var inChan = (int)((((uint)inPixel) >> shift) & 0xFF);
		var setChan = (int)((((uint)setPixel) >> shift) & 0xFF);
		var blend = setChan * n + inChan * (m - n);
		var component = (((uint)(blend / m)) & 0xFF) << shift;
		return component;
		*/
		var inChan = (long)(((uint)inPixel) & mask);
			var setChan = (long)(((uint)setPixel) & mask);
			var blend = setChan * n + inChan * (m - n);
			var component = ((uint)(blend / m)) & mask;
			return component;
			}
#endif // XBRZ_WIDE_BLEND
	}
}

sealed class Scaler2X : IScaler {
	internal new const int Scale = 2;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Scaler2X(in Config config) : base(Scale, config) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineShallow(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 4, ref matrix[Scale - 1, 0], color);
		AlphaBlend(3, 4, ref matrix[Scale - 1, 1], color);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineSteep(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 4, ref matrix[0, Scale - 1], color);
		AlphaBlend(3, 4, ref matrix[1, Scale - 1], color);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineSteepAndShallow(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 4, ref matrix[1, 0], color);
		AlphaBlend(1, 4, ref matrix[0, 1], color);
		AlphaBlend(5, 6, ref matrix[1, 1], color); //[!] fixes 7/8 used in xBR
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineDiagonal(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 2, ref matrix[1, 1], color);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendCorner(Color8 color, ref OutputMatrix matrix) {
		//model a round corner
		AlphaBlend(21, 100, ref matrix[1, 1], color); //exact: 1 - pi/4 = 0.2146018366
	}
}

sealed class Scaler3X : IScaler {
	internal new const int Scale = 3;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Scaler3X(in Config config) : base(Scale, config) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineShallow(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 4, ref matrix[Scale - 1, 0], color);
		AlphaBlend(1, 4, ref matrix[Scale - 2, 2], color);
		AlphaBlend(3, 4, ref matrix[Scale - 1, 1], color);
		matrix[Scale - 1, 2] = color;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineSteep(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 4, ref matrix[0, Scale - 1], color);
		AlphaBlend(1, 4, ref matrix[2, Scale - 2], color);
		AlphaBlend(3, 4, ref matrix[1, Scale - 1], color);
		matrix[2, Scale - 1] = color;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineSteepAndShallow(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 4, ref matrix[2, 0], color);
		AlphaBlend(1, 4, ref matrix[0, 2], color);
		AlphaBlend(3, 4, ref matrix[2, 1], color);
		AlphaBlend(3, 4, ref matrix[1, 2], color);
		matrix[2, 2] = color;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineDiagonal(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 8, ref matrix[1, 2], color);
		AlphaBlend(1, 8, ref matrix[2, 1], color);
		AlphaBlend(7, 8, ref matrix[2, 2], color);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendCorner(Color8 color, ref OutputMatrix matrix) {
		//model a round corner
		AlphaBlend(45, 100, ref matrix[2, 2], color); //exact: 0.4545939598
																									//alphaBlend(14, 1000, out.ref(2, 1), color); //0.01413008627 -> negligable
																									//alphaBlend(14, 1000, out.ref(1, 2), color); //0.01413008627
	}
}

sealed class Scaler4X : IScaler {
	internal new const int Scale = 4;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Scaler4X(in Config config) : base(Scale, config) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineShallow(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 4, ref matrix[Scale - 1, 0], color);
		AlphaBlend(1, 4, ref matrix[Scale - 2, 2], color);
		AlphaBlend(3, 4, ref matrix[Scale - 1, 1], color);
		AlphaBlend(3, 4, ref matrix[Scale - 2, 3], color);
		matrix[Scale - 1, 2] = color;
		matrix[Scale - 1, 3] = color;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineSteep(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 4, ref matrix[0, Scale - 1], color);
		AlphaBlend(1, 4, ref matrix[2, Scale - 2], color);
		AlphaBlend(3, 4, ref matrix[1, Scale - 1], color);
		AlphaBlend(3, 4, ref matrix[3, Scale - 2], color);
		matrix[2, Scale - 1] = color;
		matrix[3, Scale - 1] = color;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineSteepAndShallow(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(3, 4, ref matrix[3, 1], color);
		AlphaBlend(3, 4, ref matrix[1, 3], color);
		AlphaBlend(1, 4, ref matrix[3, 0], color);
		AlphaBlend(1, 4, ref matrix[0, 3], color);
		AlphaBlend(1, 3, ref matrix[2, 2], color); //[!] fixes 1/4 used in xBR
		matrix[3, 3] = color;
		matrix[3, 2] = color;
		matrix[2, 3] = color;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineDiagonal(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 2, ref matrix[Scale - 1, Scale / 2], color);
		AlphaBlend(1, 2, ref matrix[Scale - 2, Scale / 2 + 1], color);
		matrix[Scale - 1, Scale - 1] = color;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendCorner(Color8 color, ref OutputMatrix matrix) {
		//model a round corner
		AlphaBlend(68, 100, ref matrix[3, 3], color); //exact: 0.6848532563
		AlphaBlend(9, 100, ref matrix[3, 2], color); //0.08677704501
		AlphaBlend(9, 100, ref matrix[2, 3], color); //0.08677704501
	}
}

sealed class Scaler5X : IScaler {
	internal new const int Scale = 5;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Scaler5X(in Config config) : base(Scale, config) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineShallow(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 4, ref matrix[Scale - 1, 0], color);
		AlphaBlend(1, 4, ref matrix[Scale - 2, 2], color);
		AlphaBlend(1, 4, ref matrix[Scale - 3, 4], color);
		AlphaBlend(3, 4, ref matrix[Scale - 1, 1], color);
		AlphaBlend(3, 4, ref matrix[Scale - 2, 3], color);
		matrix[Scale - 1, 2] = color;
		matrix[Scale - 1, 3] = color;
		matrix[Scale - 1, 4] = color;
		matrix[Scale - 2, 4] = color;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineSteep(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 4, ref matrix[0, Scale - 1], color);
		AlphaBlend(1, 4, ref matrix[2, Scale - 2], color);
		AlphaBlend(1, 4, ref matrix[4, Scale - 3], color);
		AlphaBlend(3, 4, ref matrix[1, Scale - 1], color);
		AlphaBlend(3, 4, ref matrix[3, Scale - 2], color);
		matrix[2, Scale - 1] = color;
		matrix[3, Scale - 1] = color;
		matrix[4, Scale - 1] = color;
		matrix[4, Scale - 2] = color;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineSteepAndShallow(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 4, ref matrix[0, Scale - 1], color);
		AlphaBlend(1, 4, ref matrix[2, Scale - 2], color);
		AlphaBlend(3, 4, ref matrix[1, Scale - 1], color);
		AlphaBlend(1, 4, ref matrix[Scale - 1, 0], color);
		AlphaBlend(1, 4, ref matrix[Scale - 2, 2], color);
		AlphaBlend(3, 4, ref matrix[Scale - 1, 1], color);
		AlphaBlend(2, 3, ref matrix[3, 3], color);
		matrix[2, Scale - 1] = color;
		matrix[3, Scale - 1] = color;
		matrix[Scale - 1, 2] = color;
		matrix[Scale - 1, 3] = color;
		matrix[4, Scale - 1] = color;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineDiagonal(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 8, ref matrix[Scale - 1, Scale / 2], color);
		AlphaBlend(1, 8, ref matrix[Scale - 2, Scale / 2 + 1], color);
		AlphaBlend(1, 8, ref matrix[Scale - 3, Scale / 2 + 2], color);
		AlphaBlend(7, 8, ref matrix[4, 3], color);
		AlphaBlend(7, 8, ref matrix[3, 4], color);
		matrix[4, 4] = color;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendCorner(Color8 color, ref OutputMatrix matrix) {
		//model a round corner
		AlphaBlend(86, 100, ref matrix[4, 4], color); //exact: 0.8631434088
		AlphaBlend(23, 100, ref matrix[4, 3], color); //0.2306749731
		AlphaBlend(23, 100, ref matrix[3, 4], color); //0.2306749731
																									//AlphaBlend(8, 1000, ref matrix[4, 2], color); //0.008384061834 -> negligable
																									//AlphaBlend(8, 1000, ref matrix[2, 4], color); //0.008384061834
	}
}

sealed class Scaler6X : IScaler {
	internal new const int Scale = 6;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Scaler6X(in Config config) : base(Scale, config) { }

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineShallow(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 4, ref matrix[Scale - 1, 0], color);
		AlphaBlend(1, 4, ref matrix[Scale - 2, 2], color);
		AlphaBlend(1, 4, ref matrix[Scale - 3, 4], color);
		AlphaBlend(3, 4, ref matrix[Scale - 1, 1], color);
		AlphaBlend(3, 4, ref matrix[Scale - 2, 3], color);
		AlphaBlend(3, 4, ref matrix[Scale - 3, 5], color);

		matrix[Scale - 1, 2] = color;
		matrix[Scale - 1, 3] = color;
		matrix[Scale - 1, 4] = color;
		matrix[Scale - 2, 5] = color;
		matrix[Scale - 2, 4] = color;
		matrix[Scale - 2, 5] = color;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineSteep(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 4, ref matrix[0, Scale - 1], color);
		AlphaBlend(1, 4, ref matrix[2, Scale - 2], color);
		AlphaBlend(1, 4, ref matrix[4, Scale - 3], color);
		AlphaBlend(3, 4, ref matrix[1, Scale - 1], color);
		AlphaBlend(3, 4, ref matrix[3, Scale - 2], color);
		AlphaBlend(3, 4, ref matrix[5, Scale - 3], color);

		matrix[2, Scale - 1] = color;
		matrix[3, Scale - 1] = color;
		matrix[4, Scale - 1] = color;
		matrix[5, Scale - 1] = color;
		matrix[4, Scale - 2] = color;
		matrix[5, Scale - 2] = color;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineSteepAndShallow(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 4, ref matrix[0, Scale - 1], color);
		AlphaBlend(1, 4, ref matrix[2, Scale - 2], color);
		AlphaBlend(3, 4, ref matrix[1, Scale - 1], color);
		AlphaBlend(3, 4, ref matrix[3, Scale - 2], color);
		AlphaBlend(1, 4, ref matrix[Scale - 1, 0], color);
		AlphaBlend(1, 4, ref matrix[Scale - 2, 2], color);
		AlphaBlend(3, 4, ref matrix[Scale - 1, 1], color);
		AlphaBlend(3, 4, ref matrix[Scale - 2, 3], color);

		matrix[2, Scale - 1] = color;
		matrix[3, Scale - 1] = color;
		matrix[4, Scale - 1] = color;
		matrix[5, Scale - 1] = color;
		matrix[4, Scale - 2] = color;
		matrix[5, Scale - 2] = color;
		matrix[Scale - 1, 2] = color;
		matrix[Scale - 1, 3] = color;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendLineDiagonal(Color8 color, ref OutputMatrix matrix) {
		AlphaBlend(1, 2, ref matrix[Scale - 1, Scale / 2], color);
		AlphaBlend(1, 2, ref matrix[Scale - 2, Scale / 2 + 1], color);
		AlphaBlend(1, 2, ref matrix[Scale - 3, Scale / 2 + 2], color);

		matrix[Scale - 2, Scale - 1] = color;
		matrix[Scale - 1, Scale - 1] = color;
		matrix[Scale - 1, Scale - 2] = color;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal override void BlendCorner(Color8 color, ref OutputMatrix matrix) {
		//model a round corner
		AlphaBlend(97, 100, ref matrix[5, 5], color); //exact: 0.9711013910
		AlphaBlend(42, 100, ref matrix[4, 5], color); //0.4236372243
		AlphaBlend(42, 100, ref matrix[5, 4], color); //0.4236372243
		AlphaBlend(6, 100, ref matrix[5, 3], color); //0.05652034508
		AlphaBlend(6, 100, ref matrix[3, 5], color); //0.05652034508
	}
}
