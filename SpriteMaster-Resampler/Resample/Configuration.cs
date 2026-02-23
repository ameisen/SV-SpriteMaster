using JetBrains.Annotations;

// ReSharper disable ConvertToConstant.Global

namespace SpriteMaster.Resample;

internal struct Configuration() {
	//private readonly Flags AllFlags = DefaultFlags;

	public int PreferredMaxTextureDimension = 16_384;
	public int ClampDimension = 4096;

	public struct ResampleInfo() {
		public bool Enabled = true;
		public bool PremultiplyAlpha = true;
		public bool AssumeGammaCorrected = true;
		public bool EnableWrappedAddressing = false;
		public bool Scale = true;
		public bool UseColorEnhancement = true;
		public bool TrimExcessTransparency = false;
		public bool UseRedmean = false;
		[ValueRange(0, ushort.MaxValue)]
		public ushort PremultiplicationLowPass = 1023;

		public struct RecolorInfo() {
			public bool Enabled = false;
			public double RScalar = 0.897642;
			public double GScalar = 0.998476;
			public double BScalar = 1.18365;
		}

		public RecolorInfo Recolor = new();

		public struct AnalysisInfo() {
			public int MinimumGradientShades = 2;
			public int MaxGradientColorDifference = 38;
			public double MinimumPremultipliedOpaqueProportion = 0.05;
			public double MaximumGradientOpaqueProportion = 0.95;

			public struct BlockMultipleInfo() {
				public bool Enabled = true;
				public int EqualityThreshold = 1;
			}

			public BlockMultipleInfo BlockMultiple = new();

			public struct WrapDetectionInfo() {
				public bool Enabled = true;
				public float EdgeThreshold = 0.2f;
				public byte AlphaThreshold = 1;
			}

			public WrapDetectionInfo WrapDetection = new();
		}

		public AnalysisInfo Analysis = new();

		public struct DeposterizationInfo() {
			public bool PreEnabled = false;
			public bool PostEnabled = false;
			public bool UsePerceptualColor = false;
			public bool UseRedmean = false;
			public int Passes = 2;
			public int Threshold = 32;
			public int BlockSize = 1;
		}

		public DeposterizationInfo Deposterization = new();

		public struct PaddingInfo() {
			public bool Enabled = true;
			public bool PadSolidEdges = false;
			public bool IgnoreUnknown = false;
			public int MinimumSizeTexels = 4;
		}

		public PaddingInfo Padding = new();

		public struct CommonInfo() {
			public byte EqualColorTolerance = 20;
			public double LuminanceWeight = 1.0;
		}

		public CommonInfo Common = new();

		// ReSharper disable once InconsistentNaming
		public struct xBRZInfo() {
			public double DominantDirectionThreshold = 4.4;
			public double SteepDirectionThreshold = 2.2;
			public double CenterDirectionBias = 3.0;
			public bool UseGradientBlockCopy = false;
		}

		// ReSharper disable once InconsistentNaming
		public xBRZInfo xBRZ = new();
	}

	public ResampleInfo Resample = new();
};