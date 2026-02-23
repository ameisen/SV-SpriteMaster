namespace SpriteMaster.Resample;

internal sealed partial class Resampler {
	internal static Scaler CurrentConfiguredScaler => Scaler.xBRZ;

	// ReSharper disable once UnusedParameter.Local
	private static int? OverrideBlockSize(SpriteInfoBase info) {
		return null;
	}
}