namespace SpriteMaster.Tools.Preview.Preview;

internal sealed class EpxPreviewWindowConfig : PreviewWindowConfig {
	internal EpxPreviewWindowConfig(PreviewWindow window) :
		base(window, ResamplerType.Epx, typeof(Resample.Scalers.EPX.Config)) {

	}

	public override void Dispose() {
		base.Dispose();
	}
}
