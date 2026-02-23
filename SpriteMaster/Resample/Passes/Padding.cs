using LinqFasterer;
using SpriteMaster.Extensions;
using SpriteMaster.Types;

namespace SpriteMaster.Resample.Passes;

internal static partial class Padding {
	private static bool OverridePaddingExt(
		SpriteInfoBase input
	) {
		return 
			input is SpriteInfo spriteInfo &&
			SMConfig.Resample.Padding.AlwaysList.AnyF(prefix => spriteInfo.Reference.NormalizedName().StartsWith(prefix));
	}

	internal static bool IsBlacklisted(Bounds bounds, XTexture2D reference) {
		var normalizedName = reference.NormalizedName();

		foreach (var blacklistedRef in SMConfig.Resample.Padding.BlackListS) {
			if (!blacklistedRef.Pattern.IsMatch(normalizedName)) {
				continue;
			}
			if (blacklistedRef.Bounds.IsEmpty || blacklistedRef.Bounds.Contains(bounds)) {
				return true;
			}
		}

		return false;
	}
}
