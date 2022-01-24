using SpriteMaster.Resample.Scalers.SuperXBR.Cg;
using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

using static SpriteMaster.Resample.Scalers.SuperXBR.Cg.CgMath;

namespace SpriteMaster.Resample.Scalers.SuperXBR.Passes;

sealed class Pass0 : Pass {
	internal Pass0(Config config, Vector2I sourceSize, Vector2I targetSize) : base(config, sourceSize, targetSize) { }

	private float Weight1 => Configuration.Weight * 1.29633f / 10.0f;
	private float Weight2 => Configuration.Weight * 1.75068f / 10.0f / 2.0f;

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

	private static float WeightedDifferenceDiagonal(float b0, float b1, float c0, float c1, float c2, float d0, float d1, float d2, float d3, float e1, float e2, float e3, float f2, float f3) {
		return (
			PixelWeights[0] * (Difference(c1, c2) + Difference(c1, c0) + Difference(e2, e1) + Difference(e2, e3)) +
			PixelWeights[1] * (Difference(d2, d3) + Difference(d0, d1)) +
			PixelWeights[2] * (Difference(d1, d3) + Difference(d0, d2)) +
			PixelWeights[3] * Difference(d1, d2) +
			PixelWeights[4] * (Difference(c0, c2) + Difference(e1, e3)) +
			PixelWeights[5] * (Difference(b0, b1) + Difference(f2, f3))
		);
	}

	private static float WeightedDifferenceHorizontalVertical(float i1, float i2, float i3, float i4, float e1, float e2, float e3, float e4) {
		return (
			PixelWeights[3] * (Difference(i1, i2) + Difference(i3, i4)) +
			PixelWeights[0] * (Difference(i1, e1) + Difference(i2, e2) + Difference(i3, e3) + Difference(i4, e4))
		);
	}

	//           X   Y   Z   W
	// VAR.t1 = -1, -1,  2,  2
	// VAR.t2 =  0, -1,  1,  2
	// VAR.t3 = -1,  0,  2,  1
	// VAR.t4 =  0,  0,  1,  1

	private static readonly Float4[] UV = new Float4[] {
		(-1.0f, -1.0f,  2.0f,  2.0f),
		( 0.0f, -1.0f,  1.0f,  2.0f),
		(-1.0f,  0.0f,  2.0f,  1.0f),
		( 0.0f,  0.0f,  1.0f,  1.0f)
	};

	// Pass-0 does not resize - it is a prefiltering pass
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal void Pass(ReadOnlySpan<Float4> sourceData, Span<Float4> target) {
		var source = new Texture(this, sourceData, SourceSize);
		
		for (int y = 0; y < TargetSize.Height; ++y) {
			int yOffset = GetY(y, TargetSize) * TargetSize.Width;
			for (int x = 0; x < TargetSize.Width; ++x) {
				Vector2I texCoord = (x, y);
				int targetOffset = yOffset + GetX(x, TargetSize);

				var P0 = source.Sample(texCoord + UV[0].XY.AsInt).RGB; // xy
				var P1 = source.Sample(texCoord + UV[0].ZY.AsInt).RGB; // zy
				var P2 = source.Sample(texCoord + UV[0].XW.AsInt).RGB; // xw
				var P3 = source.Sample(texCoord + UV[0].ZW.AsInt).RGB; // zw

				var B =  source.Sample(texCoord + UV[1].XY.AsInt).RGB; // xy
				var C =  source.Sample(texCoord + UV[1].ZY.AsInt).RGB; // zy
				var H5 = source.Sample(texCoord + UV[1].XW.AsInt).RGB; // xw
				var I5 = source.Sample(texCoord + UV[1].ZW.AsInt).RGB; // zw

				var D =  source.Sample(texCoord + UV[2].XY.AsInt).RGB; // xy
				var F4 = source.Sample(texCoord + UV[2].ZY.AsInt).RGB; // zy
				var G =  source.Sample(texCoord + UV[2].XW.AsInt).RGB; // xw
				var I4 = source.Sample(texCoord + UV[2].ZW.AsInt).RGB; // zw

				var EE = source.Sample(texCoord + UV[3].XY.AsInt);     // xy
				var F =  source.Sample(texCoord + UV[3].ZY.AsInt).RGB; // zy
				var H =  source.Sample(texCoord + UV[3].XW.AsInt).RGB; // xw
				var I =  source.Sample(texCoord + UV[3].ZW.AsInt).RGB; // zw
				var E =  EE.RGB;

				float b = RGBToYUV(B);
				float c = RGBToYUV(C);
				float d = RGBToYUV(D);
				float e = RGBToYUV(E);
				float f = RGBToYUV(F);
				float g = RGBToYUV(G);
				float h = RGBToYUV(H);
				float i = RGBToYUV(I);

				float i4 = RGBToYUV(I4); float p0 = RGBToYUV(P0);
				float i5 = RGBToYUV(I5); float p1 = RGBToYUV(P1);
				float h5 = RGBToYUV(H5); float p2 = RGBToYUV(P2);
				float f4 = RGBToYUV(F4); float p3 = RGBToYUV(P3);

				// Calc edgeness in diagonal directions.
				float dEdge =
					WeightedDifferenceDiagonal(d, b, g, e, c, p2, h, f, p1, h5, i, f4, i5, i4) -
					WeightedDifferenceDiagonal(c, f4, b, f, i4, p0, e, i, p3, d, h, i5, g, h5);

				// Calc edgeness in horizontal/vertical directions.
				float hvEdge =
					WeightedDifferenceHorizontalVertical(f, i, e, h, c, i5, b, h5) -
					WeightedDifferenceHorizontalVertical(e, f, h, i, d, f4, g, i4);

				float limits = Configuration.EdgeStrength + 0.000001f;
				float edgeStrength = SmoothStep(0.0f, limits, Math.Abs(dEdge));

				// Filter weights. Two taps only.
				Float4 w1 = (-Weight1, Weight1 + 0.5f, Weight1 + 0.5f, -Weight1);
				Float4 w2 = (-Weight2, Weight2 + 0.25f, Weight2 + 0.25f, -Weight2);

				// Filtering and normalization in four direction generating four colors.
				Float3 c1 = MatrixMul(w1, P2, H, F, P1);
				Float3 c2 = MatrixMul(w1, P0, E, I, P3);
				Float3 c3 = MatrixMul(w2, D + G, E + H, F + I, F4 + I4);
				Float3 c4 = MatrixMul(w2, C + B, F + E, I + H, I5 + H5);

				float alpha = EE.A;

				// Smoothly blends the two strongest directions (one in diagonal and the other in vert/horiz direction).
				Float3 color = Lerp(
					Lerp(c1, c2, Step(0.0f, dEdge)),
					Lerp(c3, c4, Step(0.0f, hvEdge)),
					1.0f - edgeStrength
				);

				// Anti-ringing code.
				Float3 minSample =
					Min4(E, F, H, I) +
					(1.0f - Configuration.AntiRinging) *
					Lerp(
						(P2 - H) * (F - P1),
						(P0 - E) * (I - P3),
						Step(0.0f, dEdge)
					);
				Float3 maxSample =
					Max4(E, F, H, I) -
					(1.0f - Configuration.AntiRinging) *
					Lerp(
						(P2 - H) * (F - P1),
						(P0 - E) * (I - P3),
						Step(0.0f, dEdge)
					);
				color = color.Clamp(minSample, maxSample);

				target[targetOffset] = new(color, alpha);
			}
		}
	}
}
