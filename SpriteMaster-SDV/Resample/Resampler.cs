namespace SpriteMaster.Resample;

internal sealed partial class Resampler {
	private static int? OverrideBlockSize(SpriteInfoBase info) {
		if (info.IsWater || (info is SpriteInfo spriteInfo && spriteInfo.Reference == StardewValley.Game1.rainTexture)) {
			return WaterBlock;
		}

		return null;
	}
}
