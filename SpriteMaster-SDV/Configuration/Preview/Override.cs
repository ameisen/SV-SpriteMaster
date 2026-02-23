namespace SpriteMaster.Configuration.Preview;

internal class Override {
	internal static Override? Instance = null;

	internal bool Enabled = false;
	internal bool ResampleEnabled = false;
	internal Resample.Scaler Scaler = Resample.Scaler.None;
	internal Resample.Scaler ScalerPortrait = Resample.Scaler.None;
	internal Resample.Scaler ScalerText = Resample.Scaler.None;
	internal Resample.Scaler ScalerGradient = Resample.Scaler.None;
	internal bool ResampleSprites = false;
	internal bool ResamplePortraits = false;
	internal bool ResampleLargeText = false;
	internal bool ResampleSmallText = false;

	// draw state
	internal bool SetLinearUnresampled = false;
	internal bool SetLinear = true;

#pragma warning disable CS0618 // Type or member is obsolete
	internal static Override FromConfig => new() {
		Enabled = SMConfig.IsUnconditionallyEnabled,
		ResampleEnabled = SMConfig.Resample.Enabled,
		Scaler = SMConfig.Resample.Scaler,
		ScalerPortrait = SMConfig.Resample.ScalerPortrait,
		ScalerText = SMConfig.Resample.ScalerText,
		ScalerGradient = SMConfig.Resample.ScalerGradient,
		ResampleSprites = SMConfig.Resample.EnabledSprites,
		ResamplePortraits = SMConfig.Resample.EnabledPortraits,
		ResampleLargeText = SMConfig.Resample.EnabledLargeText,
		ResampleSmallText = SMConfig.Resample.EnabledSmallText,

		SetLinearUnresampled = SMConfig.DrawState.SetLinearUnresampled,
		SetLinear = SMConfig.DrawState.SetLinear
	};
#pragma warning restore CS0618 // Type or member is obsolete
}
