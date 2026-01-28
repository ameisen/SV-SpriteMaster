#if !SHIPPING
namespace SpriteMaster.Resample.Scalers.SuperXBR;

sealed class ScalerInfo : IScalerInfo {
	internal static readonly ScalerInfo Instance = new();

	public Resample.Scaler Scaler => Resample.Scaler.SuperXBR;
	public int MinScale => 1;
	public int MaxScale => Config.MaxScale;
	public XGraphics.TextureFilter Filter => XGraphics.TextureFilter.Point;
	public bool PremultiplyAlpha => false;
	public bool GammaCorrect => false;
	public bool BlockCompress => false;

	public IScaler Interface => SuperXBR.Scaler.ScalerInterface.Instance;

	private ScalerInfo() { }
}
#endif
