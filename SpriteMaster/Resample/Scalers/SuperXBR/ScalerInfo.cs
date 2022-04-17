#if !SHIPPING
namespace SpriteMaster.Resample.Scalers.SuperXBR;

sealed class ScalerInfo : IScalerInfo {
	internal static readonly ScalerInfo Instance = new();

	public Resample.Scaler Scaler => Resample.Scaler.SuperXBR;
	public int MinScale => 1;
	public int MaxScale => Config.MaxScale;
	public XNA.Graphics.TextureFilter Filter => XNA.Graphics.TextureFilter.Linear;
	public bool PremultiplyAlpha => true;
	public bool GammaCorrect => true;
	public bool BlockCompress => true;

	public IScaler Interface => SuperXBR.Scaler.ScalerInterface.Instance;

	private ScalerInfo() { }
}
#endif
