using SpriteMaster.Types;
using System.Runtime.CompilerServices;
using static SpriteMaster.Colors.ColorHelpers;

namespace SpriteMaster.Resample.Scalers.xBRZ.Color;

internal sealed class ColorComparer {
	private readonly Config Configuration;
	private readonly YccConfig YccConfiguration;

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal ColorComparer(Config cfg) {
		Configuration = cfg;
		YccConfiguration = new() {
			LuminanceWeight = Configuration.LuminanceWeight,
			ChrominanceWeight = Configuration.ChrominanceWeight
		};
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal uint ColorDistance(Color16 pix1, Color16 pix2) {
		return Resample.Scalers.Common.ColorDistance(
			useRedmean: Configuration.UseRedmean,
			gammaCorrected: Configuration.GammaCorrected,
			hasAlpha: Configuration.HasAlpha,
			pix1: pix1,
			pix2: pix2,
			yccConfig: YccConfiguration
		);
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal bool IsColorEqual(Color16 color1, Color16 color2) => ColorDistance(color1, color2) < Configuration.EqualColorTolerance;
}
