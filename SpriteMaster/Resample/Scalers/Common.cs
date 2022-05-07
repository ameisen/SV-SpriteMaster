using SpriteMaster.Colors;
using SpriteMaster.Types;
using static SpriteMaster.Colors.ColorHelpers;

namespace SpriteMaster.Resample.Scalers;

internal static class Common {
	internal static uint ColorDistance(
		bool useRedmean,
		bool gammaCorrected,
		bool hasAlpha,
		in Color16 pix1,
		in Color16 pix2,
		in YccConfig yccConfig
	) {
		if (useRedmean) {
			return ColorHelpers.RedmeanDifference(
				pix1,
				pix2,
				linear: !gammaCorrected,
				alpha: hasAlpha
			);
		}
		else {
			return ColorHelpers.YccDifference(
				pix1,
				pix2,
				config: yccConfig,
				linear: !gammaCorrected,
				alpha: hasAlpha
			);
		}
	}
}
