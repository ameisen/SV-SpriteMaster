using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Resample.Passes;

internal static partial class Analysis {
	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal static LegacyResults AnalyzeLegacy(XTexture2D? reference, ReadOnlySpan<Color8> data, Bounds bounds, Vector2B wrapped) {
		return AnalyzeLegacy(
			data: data,
			bounds: bounds,
			wrapped: wrapped,
			strict: reference is not null && !reference.Anonymous() && SMConfig.Resample.Padding.StrictList.Contains(reference.NormalizedName())
		);
	}
}
