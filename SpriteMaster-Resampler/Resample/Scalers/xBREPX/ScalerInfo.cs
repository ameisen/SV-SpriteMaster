namespace SpriteMaster.Resample.Scalers.xBREPX;

internal sealed class ScalerInfo : IScalerInfo {
	internal static readonly ScalerInfo Instance = new();

	public ScalerEnum Scaler => ScalerEnum.xBREPX;
	public int MinScale => 1;
	public int MaxScale => Config.MaxScale;
#if !SM_LIBRARY
	public XGraphics.TextureFilter Filter => XGraphics.TextureFilter.Linear;
#endif
	public bool PremultiplyAlpha => true;
	public bool GammaCorrect => true;
	public bool BlockCompress => true;

	public IScaler Interface => xBREPX.Scaler.ScalerInterface.Instance;

	private ScalerInfo() { }
}
