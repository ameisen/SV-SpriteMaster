namespace SpriteMaster.Resample.Scalers.xBRZ;

sealed class ScalerInfo : IScalerInfo {
	internal static readonly ScalerInfo Instance = new();

	public Resampler.Scaler Scaler => Resampler.Scaler.SuperXBR;
	public int MinScale => 1;
	public int MaxScale => Config.MaxScale;
	public XNA.Graphics.TextureFilter Filter => XNA.Graphics.TextureFilter.Linear;
	public bool PremultiplyAlpha => true;
	public bool GammaCorrect => true;
	public bool BlockCompress => true;

	private ScalerInfo() { }
}
