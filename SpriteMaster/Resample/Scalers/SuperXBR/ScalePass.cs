using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;
using Float3 = System.Numerics.Vector3;
using Float4 = System.Numerics.Vector4;

namespace SpriteMaster.Resample.Scalers.SuperXBR;
sealed partial class Scaler {
	// https://github.com/libretro/common-shaders/blob/master/xbr/super-xbr-6p-small-details.cgp
	// https://github.com/libretro/common-shaders/blob/master/xbr/shaders/super-xbr/super-xbr-small-details-pass0.cg
	// https://github.com/libretro/common-shaders/blob/master/xbr/shaders/super-xbr/super-xbr-small-details-pass1.cg
	// https://github.com/libretro/common-shaders/blob/master/xbr/shaders/super-xbr/super-xbr-small-details-pass2.cg

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private void Scale(ReadOnlySpan<Color16> source, Vector2I sourceSize, Span<Color16> target, Vector2I targetSize) {
		
	}

	private static readonly Float3 Y = new(0.2126f, 0.7152f, 0.0722f);

	private static float RGBToYUV(in Float3 color) => Float3.Dot(color, Y);

	private static float Difference(float a, float b) => Math.Abs(a - b);

	/*
                              P1
     |P0|B |C |P1|         C     F4          |a0|b1|c2|d3|
     |D |E |F |F4|      B     F     I4       |b0|c1|d2|e3|   |e1|i1|i2|e2|
     |G |H |I |I4|   P0    E  A  I     P3    |c0|d1|e2|f3|   |e3|i3|i4|e4|
     |P2|H5|I5|P3|      D     H     I5       |d0|e1|f2|g3|
                           G     H5
                              P2
	*/

	private static readonly float[] PixelWeights = new float[] { 1.0f, 0.0f, 0.0f, 1.0f, -1.0f, 0.0f };

	private static float d_wd(float b0, float b1, float c0, float c1, float c2, float d0, float d1, float d2, float d3, float e1, float e2, float e3, float f2, float f3) {
		return (
			PixelWeights[1] * (Difference(c1, c2) + Difference(c1, c0) + Difference(e2, e1) + Difference(e2, e3)) +
			PixelWeights[2] * (Difference(d2, d3) + Difference(d0, d1)) +
			PixelWeights[3] * (Difference(d1, d3) + Difference(d0, d2)) +
			PixelWeights[4] * Difference(d1, d2) +
			PixelWeights[5] * (Difference(c0, c2) + Difference(e1, e3)) +
			PixelWeights[6] * (Difference(b0, b1) + Difference(f2, f3))
		);
	}

	private static float hv_wd(float i1, float i2, float i3, float i4, float e1, float e2, float e3, float e4) {
		return (
			PixelWeights[4] * (Difference(i1, i2) + Difference(i3, i4)) +
			PixelWeights[1] * (Difference(i1, e1) + Difference(i2, e2) + Difference(i3, e3) + Difference(i4, e4))
		);
	}

	private static Float3 Min4(in Float3 a, in Float3 b, in Float3 c, in Float3 d) {
		return Float3.Min(a, Float3.Min(b, Float3.Min(c, d)));
	}

	private static Float3 Max4(in Float3 a, in Float3 b, in Float3 c, in Float3 d) {
		return Float3.Max(a, Float3.Max(b, Float3.Max(c, d)));
	}

	// VAR.t1 = -1, -1, 2, 2
	// VAR.t2 =  0, -1, 1, 2
	// VAR.t3 = -1,  0, 2, 1
	// VAR.t4 =  0,  0, 1, 1

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private void Pass0(ReadOnlySpan<Color16> source, Vector2I sourceSize, Span<Color16> target, Vector2I targetSize) {
		for (int y = 0; y < targetSize.Height; ++y) {
			for (int x = 0; x < targetSize.Width; ++x) {
				
				//Float4 P0 = 
			}
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private void Pass1(ReadOnlySpan<Color16> source, Vector2I sourceSize, Span<Color16> target, Vector2I targetSize) {
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private void Pass2(ReadOnlySpan<Color16> source, Vector2I sourceSize, Span<Color16> target, Vector2I targetSize) {
	}
}
