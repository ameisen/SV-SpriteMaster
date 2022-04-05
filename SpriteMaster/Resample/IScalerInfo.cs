namespace SpriteMaster.Resample;

interface IScalerInfo {
	Resampler.Scaler Scaler { get; }
	int MinScale { get; }
	int MaxScale { get; }
	XNA.Graphics.TextureFilter Filter { get; }
	bool PremultiplyAlpha { get; }
	bool GammaCorrect { get; }
	bool BlockCompress { get; }
}
