using JetBrains.Annotations;

namespace SpriteMaster;

[PublicAPI]
public readonly ref struct Configuration() {
	public int PreferredMaxTextureDimension { get; init; } = 16_384;
	public int ClampDimension { get; init; } = 4096;

	[PublicAPI]
	public readonly ref struct ResampleInfo() {
		public bool Enabled { get; init; } = true;
		public bool PremultiplyAlpha { get; init; } = true;
		public bool AssumeGammaCorrected { get; init; } = true;
		public bool EnableWrappedAddressing { get; init; } = false;
		public bool Scale { get; init; } = true;
		public bool UseColorEnhancement { get; init; } = true;
		public bool TrimExcessTransparency { get; init; } = false;
		public bool UseRedmean { get; init; } = false;
		[ValueRange(0, ushort.MaxValue)]
		public ushort PremultiplicationLowPass { get; init; } = 1023;

		[PublicAPI]
		public readonly ref struct RecolorInfo() {
			public bool Enabled { get; init; } = false;
			public double RScalar { get; init; } = 0.897642;
			public double GScalar { get; init; } = 0.998476;
			public double BScalar { get; init; } = 1.18365;
		}

		public RecolorInfo Recolor { get; init; } = new();

		[PublicAPI]
		public readonly ref struct AnalysisInfo() {
			public int MinimumGradientShades { get; init; } = 2;
			public int MaxGradientColorDifference { get; init; } = 38;
			public double MinimumPremultipliedOpaqueProportion { get; init; } = 0.05;
			public double MaximumGradientOpaqueProportion { get; init; } = 0.95;

			[PublicAPI]
			public readonly ref struct BlockMultipleInfo() {
				public bool Enabled { get; init; } = true;
				public int EqualityThreshold { get; init; } = 1;
			}

			public BlockMultipleInfo BlockMultiple { get; init; } = new();

			[PublicAPI]
			public readonly ref struct WrapDetectionInfo() {
				public bool Enabled { get; init; } = true;
				public float EdgeThreshold { get; init; } = 0.2f;
				public byte AlphaThreshold { get; init; } = 1;
			}

			public WrapDetectionInfo WrapDetection { get; init; } = new();
		}

		public AnalysisInfo Analysis { get; init; } = new();

		[PublicAPI]
		public readonly ref struct DeposterizationInfo() {
			public bool PreEnabled { get; init; } = false;
			public bool PostEnabled { get; init; } = false;
			public bool UsePerceptualColor { get; init; } = false;
			public bool UseRedmean { get; init; } = false;
			public int Passes { get; init; } = 2;
			public int Threshold { get; init; } = 32;
			public int BlockSize { get; init; } = 1;
		}

		public DeposterizationInfo Deposterization { get; init; } = new();

		[PublicAPI]
		public readonly ref struct PaddingInfo() {
			public bool Enabled { get; init; } = true;
			public bool PadSolidEdges { get; init; } = false;
			public bool IgnoreUnknown { get; init; } = false;
			public int MinimumSizeTexels { get; init; } = 4;
		}

		public PaddingInfo Padding { get; init; } = new();

		[PublicAPI]
		public readonly ref struct CommonInfo() {
			public byte EqualColorTolerance { get; init; } = 20;
			public double LuminanceWeight { get; init; } = 1.0;
		}

		public CommonInfo Common { get; init; } = new();

		// ReSharper disable once InconsistentNaming
		[PublicAPI]
		public readonly ref struct xBRZInfo() {
			public bool UseGradientBlockCopy { get; init; } = false;

			public double DominantDirectionThreshold { get; init; } = 4.4;
			public double SteepDirectionThreshold { get; init; } = 2.2;
			public double CenterDirectionBias { get; init; } = 3.0;
		}

		// ReSharper disable once InconsistentNaming
		public xBRZInfo xBRZ { get; init; } = new();
	}

	public ResampleInfo Resample { get; init; } = new();

	internal readonly Resample.Configuration InternalConfiguration =>
		new() {
			PreferredMaxTextureDimension = PreferredMaxTextureDimension,
			ClampDimension = ClampDimension,
			Resample = new() {
				Enabled = Resample.Enabled,
				PremultiplyAlpha = Resample.PremultiplyAlpha,
				AssumeGammaCorrected = Resample.AssumeGammaCorrected,
				Scale = Resample.Scale,
				UseColorEnhancement = Resample.UseColorEnhancement,
				TrimExcessTransparency = Resample.TrimExcessTransparency,
				UseRedmean = Resample.UseRedmean,
				PremultiplicationLowPass = Resample.PremultiplicationLowPass,
				Recolor = new() {
					Enabled = Resample.Recolor.Enabled,
					RScalar = Resample.Recolor.RScalar,
					GScalar = Resample.Recolor.GScalar,
					BScalar = Resample.Recolor.BScalar
				},
				Analysis = new() {
					MinimumGradientShades = Resample.Analysis.MinimumGradientShades,
					MaxGradientColorDifference = Resample.Analysis.MaxGradientColorDifference,
					MinimumPremultipliedOpaqueProportion = Resample.Analysis.MinimumPremultipliedOpaqueProportion,
					MaximumGradientOpaqueProportion = Resample.Analysis.MaximumGradientOpaqueProportion,

					BlockMultiple = new() {
						Enabled = Resample.Analysis.BlockMultiple.Enabled,
						EqualityThreshold = Resample.Analysis.BlockMultiple.EqualityThreshold
					},
					WrapDetection = new() {
						Enabled = Resample.Analysis.WrapDetection.Enabled,
						EdgeThreshold = Resample.Analysis.WrapDetection.EdgeThreshold,
						AlphaThreshold = Resample.Analysis.WrapDetection.AlphaThreshold
					}
				},
				Deposterization = new() {
					PreEnabled = Resample.Deposterization.PreEnabled,
					PostEnabled = Resample.Deposterization.PostEnabled,
					UsePerceptualColor = Resample.Deposterization.UsePerceptualColor,
					UseRedmean = Resample.Deposterization.UseRedmean,
					Passes = Resample.Deposterization.Passes,
					Threshold = Resample.Deposterization.Threshold,
					BlockSize = Resample.Deposterization.BlockSize
				},
				Padding = new() {
					Enabled = Resample.Padding.Enabled,
					PadSolidEdges = Resample.Padding.PadSolidEdges,
					IgnoreUnknown = Resample.Padding.IgnoreUnknown,
					MinimumSizeTexels = Resample.Padding.MinimumSizeTexels
				},
				Common = new() {
					EqualColorTolerance = Resample.Common.EqualColorTolerance,
					LuminanceWeight = Resample.Common.LuminanceWeight
				},
				xBRZ = new() {
					UseGradientBlockCopy = Resample.xBRZ.UseGradientBlockCopy,
					DominantDirectionThreshold = Resample.xBRZ.DominantDirectionThreshold,
					SteepDirectionThreshold = Resample.xBRZ.SteepDirectionThreshold,
					CenterDirectionBias = Resample.xBRZ.CenterDirectionBias
				}
			}
		};
}
