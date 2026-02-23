using SpriteMaster.Resample.Scalers;

namespace SpriteMaster.Resample;

internal interface IScalerInfo {
	Scaler Scaler { get; }
	int MinScale { get; }
	int MaxScale { get; }
#if !SM_LIBRARY
	XGraphics.TextureFilter Filter { get; }
#endif
	bool PremultiplyAlpha { get; }
	bool GammaCorrect { get; }
	bool BlockCompress { get; }

	IScaler Interface { get; }
}
