using SpriteMaster.Colors;
using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Resample.Passes;

internal static class GammaCorrection {
	private static readonly ColorSpace ColorSpace = ColorSpace.sRGB_Precise;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Delinearize(Span<Color16> data, in Vector2I size) {
		foreach (ref var color in data) {
			color = ColorSpace.Delinearize(color);
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Linearize(Span<Color16> data, in Vector2I size) {
		foreach (ref var color in data) {
			color = ColorSpace.Linearize(color);
		}
	}
}
