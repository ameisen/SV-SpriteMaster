using SpriteMaster.Types;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Resample.Scalers;

internal abstract class LuminanceConfig : Config {
	internal readonly double LuminanceWeight;
	internal readonly double ChrominanceWeight;

	[MethodImpl(Runtime.MethodImpl.Inline)]
	protected LuminanceConfig(
		Vector2B wrapped,
		bool hasAlpha,
		bool gammaCorrected,
		double luminanceWeight
	) : base(
		wrapped,
		hasAlpha,
		gammaCorrected
	) {
		LuminanceWeight = luminanceWeight;

		var adjustedLuminanceWeight = luminanceWeight / (luminanceWeight + 1.0);
		LuminanceWeight = adjustedLuminanceWeight * 2.0;
		ChrominanceWeight = (1.0 - adjustedLuminanceWeight) * 2.0;
	}
}
