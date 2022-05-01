namespace SpriteMaster.Configuration.Preview;

internal class Override {
	internal static Override? Instance = null;

	internal bool Enabled = false;
	internal bool ResampleEnabled = false;
	internal Resample.Scaler Scaler = Resample.Scaler.None;
	internal Resample.Scaler ScalerGradient = Resample.Scaler.None;
	internal bool ResampleSprites = false;
	internal bool ResampleText = false;
	internal bool ResampleBasicText = false;

	// draw state
	internal bool SetLinear = true;

#pragma warning disable CS0618 // Type or member is obsolete
	internal static Override FromConfig => new() {
		Enabled = Config.IsUnconditionallyEnabled,
		ResampleEnabled = Config.Resample.Enabled,
		Scaler = Config.Resample.Scaler,
		ScalerGradient = Config.Resample.ScalerGradient,
		ResampleSprites = Config.Resample.EnabledSprites,
		ResampleText = Config.Resample.EnabledText,
		ResampleBasicText = Config.Resample.EnabledBasicText,

		SetLinear = Config.DrawState.SetLinear
	};
#pragma warning restore CS0618 // Type or member is obsolete
}
