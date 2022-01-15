using SpriteMaster.Colors;
using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

#nullable enable

namespace SpriteMaster.Resample.Passes;

static class GammaCorrection {
	private static readonly ColorSpace ColorSpace = ColorSpace.sRGB_Precise;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Delinearize(Span<Color16> data, in Vector2I size) {
		foreach (ref Color16 color in data) {
			color.R = ColorSpace.Delinearize(color.R);
			color.G = ColorSpace.Delinearize(color.G);
			color.B = ColorSpace.Delinearize(color.B);
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Linearize(Span<Color16> data, in Vector2I size) {
		foreach (ref Color16 color in data) {
			color.R = ColorSpace.Linearize(color.R);
			color.G = ColorSpace.Linearize(color.G);
			color.B = ColorSpace.Linearize(color.B);
		}
	}
}
