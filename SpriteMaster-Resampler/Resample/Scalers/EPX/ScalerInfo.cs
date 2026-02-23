namespace SpriteMaster.Resample.Scalers.EPX;

internal sealed class ScalerInfo : IScalerInfo {
	internal static readonly ScalerInfo Instance = new(ScalerEnum.EPX);
	internal static readonly ScalerInfo InstanceLegacy = new(ScalerEnum.EPXLegacy);

	public ScalerEnum Scaler { get; }
	public int MinScale => 1;
	public int MaxScale => Config.MaxScale;
#if !SM_LIBRARY
	public XGraphics.TextureFilter Filter => XGraphics.TextureFilter.Point;
#endif
	public bool PremultiplyAlpha => true;
	public bool GammaCorrect => false;
	public bool BlockCompress => false;

	public IScaler Interface => (Scaler is ScalerEnum.EPX)
		? EPX.Scaler.ScalerInterface.Instance
		: EPX.Scaler.ScalerInterface.InstanceLegacy;

	private ScalerInfo(ScalerEnum scaler) {
		Scaler = scaler;
	}
}
