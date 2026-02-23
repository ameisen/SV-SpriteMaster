#if !SHIPPING
namespace SpriteMaster.Resample.Scalers.SuperXBR;

sealed class ScalerInfo : IScalerInfo {
	internal static readonly ScalerInfo Instance = new();

	public ScalerEnum Scaler => ScalerEnum.SuperXBR;
	public int MinScale => 1;
	public int MaxScale => Config.MaxScale;
#if !SM_LIBRARY
	public XGraphics.TextureFilter Filter => XGraphics.TextureFilter.Point;
#endif
	public bool PremultiplyAlpha => false;
	public bool GammaCorrect => false;
	public bool BlockCompress => false;

	public IScaler Interface => SuperXBR.Scaler.ScalerInterface.Instance;

	private ScalerInfo() { }
}
#endif
