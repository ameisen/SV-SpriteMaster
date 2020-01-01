using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;

namespace SpriteMaster.Resample {
	internal static class ColorSpace {
		// TODO : After conversion, we should be temporarily working with 64-bit textures instead of 32-bit. We want to retain precision.

		private const bool UseStrictConversion = true;
		private const bool IgnoreAlpha = true; // sRGB Alpha doesn't make much sense. I suppose it isn't impossible, but why? It's a data channel.
		private const double ByteMax = (double)byte.MaxValue;
		private const double Gamma = 2.4;

		// https://entropymine.com/imageworsener/srgbformula/
		private static double ToSRGB (double value, double gamma) {
			if (UseStrictConversion) {
				if (value <= 0.00313066844250063)
					return value * 12.92;
				else
					return 1.055 * Math.Pow(value, 1.0 / gamma) - 0.055;
			}
			else {
				return Math.Pow(value, gamma);
			}
		}

		private static double ToLinear (double value, double gamma) {
			if (UseStrictConversion) {
				if (value <= 0.0404482362771082)
					return value / 12.92;
				else
					return Math.Pow((value + 0.055) / 1.055, gamma);
			}
			else {
				return Math.Pow(value, 1.0 / gamma);
			}
		}

		internal static void ConvertSRGBToLinear (int[] textureData, Texel.Ordering order = Texel.Ordering.ABGR, double gamma = Gamma) {
			ConvertSRGBToLinear(textureData.AsSpan(), order, gamma);
		}

		internal static void ConvertSRGBToLinear (Span<int> textureData, Texel.Ordering order = Texel.Ordering.ABGR, double gamma = Gamma) {
			foreach (int i in 0..textureData.Length) {
				var texelValue = textureData[i];

				var texel = Texel.From(texelValue, order);
				var R = (double)texel.R / ByteMax;
				var G = (double)texel.G / ByteMax;
				var B = (double)texel.B / ByteMax;

				Contract.AssertLessEqual(R, 1.0);
				Contract.AssertLessEqual(G, 1.0);
				Contract.AssertLessEqual(B, 1.0);

				if (!IgnoreAlpha) {
					var A = (double)texel.A / ByteMax;
					A = ToLinear(A, gamma);
					texel.A = unchecked((byte)(A * ByteMax).NearestInt());
				}
				R = ToLinear(R, gamma);
				G = ToLinear(G, gamma);
				B = ToLinear(B, gamma);

				texel.R = unchecked((byte)(R * ByteMax).NearestInt());
				texel.G = unchecked((byte)(G * ByteMax).NearestInt());
				texel.B = unchecked((byte)(B * ByteMax).NearestInt());

				textureData[i] = texel.To(order);
			}
		}

		internal static void ConvertLinearToSRGB (int[] textureData, Texel.Ordering order = Texel.Ordering.ABGR, double gamma = Gamma) {
			ConvertLinearToSRGB(textureData.AsSpan(), order, gamma);
		}

		internal static void ConvertLinearToSRGB (Span<int> textureData, Texel.Ordering order = Texel.Ordering.ABGR, double gamma = Gamma) {
			foreach (int i in 0..textureData.Length) {
				var texelValue = textureData[i];

				var texel = Texel.From(texelValue, order);
				var R = (double)texel.R / ByteMax;
				var G = (double)texel.G / ByteMax;
				var B = (double)texel.B / ByteMax;

				Contract.AssertLessEqual(R, 1.0);
				Contract.AssertLessEqual(G, 1.0);
				Contract.AssertLessEqual(B, 1.0);

				if (!IgnoreAlpha) {
					var A = (double)texel.A / ByteMax;
					A = ToSRGB(A, gamma);
					texel.A = unchecked((byte)(A * ByteMax).NearestInt());
				}
				R = ToSRGB(R, gamma);
				G = ToSRGB(G, gamma);
				B = ToSRGB(B, gamma);

				texel.R = unchecked((byte)(R * ByteMax).NearestInt());
				texel.G = unchecked((byte)(G * ByteMax).NearestInt());
				texel.B = unchecked((byte)(B * ByteMax).NearestInt());

				textureData[i] = texel.To(order);
			}
		}
	}
}
