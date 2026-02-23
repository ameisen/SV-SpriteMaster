using SpriteMaster.Types;
using System.Runtime.CompilerServices;

// TODO : Handle X or Y-only scaling, since the game has a lot of 1xY and Xx1 sprites - 1D textures.
namespace SpriteMaster.Resample.Scalers.EPX;

internal sealed class Config : Resample.Scalers.LuminanceConfig {
	internal const int MaxScale = 4;

	internal readonly uint EqualColorTolerance;
	internal readonly bool UseRedmean;
	internal readonly bool SmoothCompare;

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal Config(
		Vector2B wrapped,
		bool hasAlpha = true,
		double luminanceWeight = 1.0,
		bool gammaCorrected = true,
		byte equalColorTolerance = 30,
		bool useRedmean = false,
		bool smoothCompare = true
	) : base(
		wrapped: wrapped,
		hasAlpha: hasAlpha,
		gammaCorrected: gammaCorrected,
		luminanceWeight: luminanceWeight
	) {
		EqualColorTolerance = (uint)equalColorTolerance << 8;
		UseRedmean = useRedmean;

		SmoothCompare = smoothCompare;
	}
}
