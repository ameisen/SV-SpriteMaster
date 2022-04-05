namespace SpriteMaster.Resample.Scalers.EPX;

sealed class ScalerInfo : IScalerInfo {
	internal static readonly ScalerInfo Instance = new();

	public Resampler.Scaler Scaler => Resampler.Scaler.EPX;
	public int MinScale => 1;
	public int MaxScale => Config.MaxScale;
	public XNA.Graphics.TextureFilter Filter => XNA.Graphics.TextureFilter.Point;
	public bool PremultiplyAlpha => true;
	public bool GammaCorrect => false;
	public bool BlockCompress => false;

	private ScalerInfo() { }
}
