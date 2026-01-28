using SpriteMaster.Resample.Scalers;

namespace SpriteMaster.Resample;

internal interface IScalerInfo {
	Scaler Scaler { get; }
	int MinScale { get; }
	int MaxScale { get; }
	XGraphics.TextureFilter Filter { get; }
	bool PremultiplyAlpha { get; }
	bool GammaCorrect { get; }
	bool BlockCompress { get; }

	IScaler Interface { get; }
}
