using SpriteMaster.Numeric;
using SpriteMaster.Resample.Scalers.SuperXBR.Cg;
using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

using static SpriteMaster.Resample.Scalers.SuperXBR.Cg.CgMath;

namespace SpriteMaster.Resample.Scalers.SuperXBR.Passes;

sealed class Pass1 : Pass {
	internal Pass1(Config config, Vector2I sourceSize, Vector2I targetSize) : base(config, sourceSize, targetSize) { }

	private float Weight1 => Configuration.Weight * 1.75068f / 10.0f;
	private float Weight2 => Configuration.Weight * 1.29633f / 10.0f / 2.0f;

	/*
																P1
			 |P0|B |C |P1|         C     F4          |a0|b1|c2|d3|
			 |D |E |F |F4|      B     F     I4       |b0|c1|d2|e3|   |e1|i1|i2|e2|
			 |G |H |I |I4|   P0    E  A  I     P3    |c0|d1|e2|f3|   |e3|i3|i4|e4|
			 |P2|H5|I5|P3|      D     H     I5       |d0|e1|f2|g3|
														 G     H5
																P2
	*/

	private static readonly float[] PixelWeights = new float[] { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

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

	// Pass-0 does not resize - it is a prefiltering pass
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal void Pass(ReadOnlySpan<Float4> sourceData, ReadOnlySpan<Float4> lastPassData, Span<Float4> target) {
		var source = new Texture(this, sourceData, SourceSize);
		var prev = new Texture(this, lastPassData, SourceSize);

		for (int y = 0; y < TargetSize.Height; ++y) {
			int yOffset = GetY(y, TargetSize) * TargetSize.Width;
			for (int x = 0; x < TargetSize.Width; ++x) {
				int targetOffset = yOffset + GetX(x, TargetSize);

				Float2 sourceOffset = (
					(x * 0.5f),
					(y * 0.5f)
				);

				Float2 fracP = CgMath.Frac(sourceOffset);
				Float2 dir = fracP - (0.5f, 0.5f);
				if ((dir.X * dir.Y) > 0.0f) {  // I'm unsure how this _ever_ would not be zero? If the inputs are half the size of the output, then xy - 0.5 is always <= zero.
					target[targetOffset] = (fracP.X > 0.5) ?
						source.Sample(sourceOffset) :
						prev.Sample(sourceOffset);
					continue;
				}

				// Skip pixels on wrong grid
				// Cannot really replicate this behavior in this implementation...
				// float2 fp = frac(VAR.texCoord * IN.texture_size);
				// float2 dir = fp - float2(0.5, 0.5);
				// if ((dir.x * dir.y) > 0.0) return (fp.x > 0.5) ? tex2D(s0, VAR.texCoord) : tex2D(PASSPREV2.texture, VAR.texCoord);

				//Float2 g1 = new Float2(0.0f, 0.5f / size.Y); // half texel, source
				//Float2 g2 = new Float2(0.5f / size.X, 0.0f); // half texel, source

				// float3 P0 = tex2D(PASSPREV2.texture, VAR.texCoord -   3.0*g1        ).xyz;
				// 

				Float2 g1 = (fracP.X > 0.5f) ? (0.5f, 0.0f) : (0.0f, 0.5f);
				Float2 g2 = (fracP.X > 0.5f) ? (0.0f, 0.5f) : (0.5f, 0.0f);

				var centroid = prev.Sample(sourceOffset);

				var P0 =   prev.Sample(sourceOffset    -3.0f*g1            ).RGB;
				var P1 = source.Sample(sourceOffset                -3.0f*g2).RGB;
				var P2 = source.Sample(sourceOffset                +3.0f*g2).RGB;
				var P3 =   prev.Sample(sourceOffset    +3.0f*g1            ).RGB;

				// float3 B = tex2D(s0, VAR.texCoord - 2.0 * g1 - g2).xyz;
				// (2 * 0.5 texels) - 0.5 texels
				// 1 texel - 0.5 texels
				// 0.5 texels should become 0 texels.
				var B =  source.Sample(sourceOffset    -2.0f*g1         -g2).RGB;
				var C =    prev.Sample(sourceOffset         -g1    -2.0f*g2).RGB;
				var D =  source.Sample(sourceOffset    -2.0f*g1         +g2).RGB;
				var E =    prev.Sample(sourceOffset         -g1            ).RGB;
				var F =  source.Sample(sourceOffset                     -g2).RGB;
				var G =    prev.Sample(sourceOffset         -g1    +2.0f*g2).RGB;
				var H =  source.Sample(sourceOffset                     +g2).RGB;
				var I =    prev.Sample(sourceOffset    +2.0f*g1         +g2).RGB;

				var F4 = source.Sample(sourceOffset         +g1    -2.0f*g2).RGB;
				var I4 =   prev.Sample(sourceOffset    +2.0f*g1         -g2).RGB;
				var H5 = source.Sample(sourceOffset         +g1    +2.0f*g2).RGB;
				var I5 =   prev.Sample(sourceOffset    +2.0f*g1         +g2).RGB;

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
				float edgeStrength = CgMath.SmoothStep(0.0f, limits, Math.Abs(dEdge));

				// Filter weights. Two taps only.
				Float4 w1 = (-Weight1, Weight1 + 0.5f, Weight1 + 0.5f, -Weight1);
				Float4 w2 = (-Weight2, Weight2 + 0.25f, Weight2 + 0.25f, -Weight2);

				// Filtering and normalization in four direction generating four colors.
				Float3 c1 = CgMath.MatrixMul(w1, P2, H, F, P1);
				Float3 c2 = CgMath.MatrixMul(w1, P0, E, I, P3);
				Float3 c3 = CgMath.MatrixMul(w2, D + G, E + H, F + I, F4 + I4);
				Float3 c4 = CgMath.MatrixMul(w2, C + B, F + E, I + H, I5 + H5);

				float alpha = centroid.A;

				// Smoothly blends the two strongest directions (one in diagonal and the other in vert/horiz direction).
				Float3 color = CgMath.Lerp(
					CgMath.Lerp(c1, c2, CgMath.Step(0.0f, dEdge)),
					CgMath.Lerp(c3, c4, CgMath.Step(0.0f, hvEdge)),
					1.0f - edgeStrength
				);

				// Anti-ringing code.
				Float3 minSample =
					CgMath.Min4(E, F, H, I) +
					(1.0f - Configuration.AntiRinging) *
					CgMath.Lerp(
						(P2 - H) * (F - P1),
						(P0 - E) * (I - P3),
						CgMath.Step(0.0f, dEdge)
					);
				Float3 maxSample =
					CgMath.Max4(E, F, H, I) -
					(1.0f - Configuration.AntiRinging) *
					CgMath.Lerp(
						(P2 - H) * (F - P1),
						(P0 - E) * (I - P3),
						CgMath.Step(0.0f, dEdge)
					);
				color = color.Clamp(minSample, maxSample);

				target[targetOffset] = new(color, alpha);
			}
		}
	}
}
