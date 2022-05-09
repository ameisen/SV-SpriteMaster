using SpriteMaster.Colors;
using SpriteMaster.Types;
using static SpriteMaster.Colors.ColorHelpers;

namespace SpriteMaster.Resample.Scalers;

internal static class Common {
	internal static uint ColorDistance(
		bool useRedmean,
		bool gammaCorrected,
		bool hasAlpha,
		Color16 pix1,
		Color16 pix2,
		in YccConfig yccConfig
	) {
		if (useRedmean) {
			return pix1.RedmeanDifference(pix2,
				linear: !gammaCorrected,
				alpha: hasAlpha
			);
		}
		else {
			return pix1.YccDifference(pix2,
				config: yccConfig,
				linear: !gammaCorrected,
				alpha: hasAlpha
			);
		}
	}
}
