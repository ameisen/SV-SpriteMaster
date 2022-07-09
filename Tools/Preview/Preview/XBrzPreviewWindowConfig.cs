namespace SpriteMaster.Tools.Preview.Preview;

internal sealed class XBrzPreviewWindowConfig : PreviewWindowConfig {
	internal XBrzPreviewWindowConfig(PreviewWindow window) :
		base(window, ResamplerType.XBrz, typeof(Resample.Scalers.xBRZ.Config)) {

	}

	public override void Dispose() {
		base.Dispose();
	}
}
