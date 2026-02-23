using SpriteMaster.Extensions;
using SpriteMaster.Resample.Passes;
using SpriteMaster.Types;
using SpriteMaster.Types.Fixed;
using SpriteMaster.Types.Spans;
using System.Runtime.InteropServices;

namespace SpriteMaster.Resample;

internal sealed partial class Resampler {
	[StructLayout(LayoutKind.Auto)]
	internal readonly ref struct ResultInfo {
		internal readonly ResampleStatus Status { get; init; } = default;
		internal readonly ReadOnlyPinnedSpan<byte> Data { get; init; } = default;
		internal readonly Vector2B Wrapped { get; init; } = default;
		internal readonly Vector2I Size { get; init; } = default;
		internal readonly PaddingQuad Padding { get; init; } = default;

		public ResultInfo() { }

		private ResultInfo(ResampleStatus status) {
			Status = status;
		}

		internal static ResultInfo FromFailure(ResampleStatus status) => new(status);
	}

	internal static unsafe ResultInfo CreateNewTextureDirect(
		BasicSpriteInfo input,
		uint scale
	) {
		if (input.ReferenceData is null) {
#pragma warning disable CA2208
			throw new ArgumentNullException(nameof(input.ReferenceData));
#pragma warning restore CA2208
		}

		var initialGammaState = GammaState.Gamma;
		var currentGammaState = initialGammaState;

		bool directImage = input.TextureType == TextureType.SlicedImage;
		Bounds inputBounds = input.Bounds;
		Bounds referenceInputBounds = inputBounds;

		// Water in the game is pre-upscaled by 4... which is weird.
		int blockSize = 1;
		if (OverrideBlockSize(input) is {} overrideBlockSize) {
			blockSize = overrideBlockSize;
		}
		/*
		else if (input.IsFont && FontBlock != 1) {
			blockSize = FontBlock;
			scale = SMConfig.Resample.MaxScale;
		}
		*/
		else if (config.Resample.Analysis.BlockMultiple.Enabled) {
			blockSize = BlockMultipleAnalysis.Analyze(
				config: config,
				data: input.ReferenceData.AsSpan<Color8>(),
				textureBounds: referenceInputBounds,
				spriteBounds: inputBounds,
				stride: input.ReferenceSize.Width
			);
		}

		if (blockSize == Math.Max(inputBounds.Width, inputBounds.Height)) {
			if (!config.Resample.Recolor.Enabled) {
				return ResultInfo.FromFailure(ResampleStatus.DisabledSolid);
			}
		}

		// TODO : handle inverted input.Bounds
		ReadOnlySpan<Color8> spriteRawData8;
		Vector2I spriteRawExtent;
		if (blockSize <= 1 && inputBounds == referenceInputBounds) {
			spriteRawData8 = input.ReferenceData.AsReadOnlySpan<Color8>();
			spriteRawExtent = inputBounds.Extent;
		}
		else {
			spriteRawData8 = ExtractSprite.Extract(
				data: input.ReferenceData.AsSpan<Color8>(),
				textureBounds: referenceInputBounds,
				spriteBounds: inputBounds,
				stride: input.ReferenceSize.Width,
				block: blockSize,
				newExtent: out spriteRawExtent
			);
		}

		// At this point, rawData includes just the sprite's raw data.

		var analysis = Analysis.AnalyzeLegacy(
			data: spriteRawData8,
			bounds: spriteRawExtent,
			wrapped: input.Wrapped
		);

		bool isGradient = analysis.MaxChannelShades >= config.Resample.Analysis.MinimumGradientShades && (analysis.GradientDiagonal.Any || analysis.GradientAxial.Any);

		bool fullPremultiplyAlpha = true;
		bool premultiplyAlpha = config.Resample.PremultiplyAlpha;
		bool gammaCorrect = config.Resample.AssumeGammaCorrected;

		double opaqueProportion = (double)analysis.OpaqueCount / spriteRawExtent.Area;
		if (!isGradient) {
			if (opaqueProportion <= config.Resample.Analysis.MinimumPremultipliedOpaqueProportion) {
				fullPremultiplyAlpha = false;
				// TODO : I want to remove this line, but it's causing a single-line artifact in wet HoeDirt that I haven't resolved yet.
				//premultiplyAlpha = false;
				//gammaCorrect = false;
			}
		}
		else {
			if (opaqueProportion >= config.Resample.Analysis.MaximumGradientOpaqueProportion) {
				isGradient = false;
			}
		}

		Scaler scalerType = input.Scaler;

		if (
			(
				!config.Resample.Recolor.Enabled &&
				!config.Resample.Enabled
			) ||
			scalerType == Scaler.None
		) {
			return ResultInfo.FromFailure(isGradient ? ResampleStatus.DisabledGradient : ResampleStatus.Disabled);
		}

		if (analysis.MaxChannelShades <= 1) {
			// If the sprite only has _one_ shade, resampling makes zero sense.
			if (!config.Resample.Recolor.Enabled) {
				return ResultInfo.FromFailure(ResampleStatus.DisabledSolid);
			}
		}

		bool resamplingAllowed = config.Resample.Enabled || CurrentConfiguredScaler == Scaler.None;

		Vector2B wrapped;
		if (config.Resample.EnableWrappedAddressing) {
			wrapped = analysis.Wrapped;
		}
		else {
			wrapped = (false, false);
		}

		var scaledSize = resamplingAllowed ? spriteRawExtent * scale : spriteRawExtent;

		// Widen data.
		var spriteRawData = Color16.Convert(spriteRawData8);

		// Apply recolor
		if (config.Resample.Recolor.Enabled) {
			float rScalar = (float)config.Resample.Recolor.RScalar;
			float gScalar = (float)config.Resample.Recolor.GScalar;
			float bScalar = (float)config.Resample.Recolor.BScalar;

			for (int i = 0; i < spriteRawData.Length; ++i) {
				ref Color16 color = ref spriteRawData[i];
				float r = Math.Clamp(color.R.RealF * rScalar, 0.0f, 1.0f);
				float g = Math.Clamp(color.G.RealF * gScalar, 0.0f, 1.0f);
				float b = Math.Clamp(color.B.RealF * bScalar, 0.0f, 1.0f);
				color.R = Fixed16.FromReal(r);
				color.G = Fixed16.FromReal(g);
				color.B = Fixed16.FromReal(b);
			}
		}

		Span<Color16> bitmapDataWide = spriteRawData;

		var scalerInfo = Scalers.IScaler.GetScalerInfo(scalerType);

		PaddingQuad padding = default;

		if (scalerInfo is not null) {
			scale *= (uint)blockSize;
			scale = Math.Clamp(scale, (uint)scalerInfo.MinScale, (uint)scalerInfo.MaxScale);

			// Adjust the scale value so that it is within the preferred dimensional limits
			if (config.Resample.Scale) {
				int preferredMaxDimension = config.PreferredMaxTextureDimension;
				var originalScale = scale;
				scale = 2;
				for (uint s = originalScale; s > 2U; --s) {
					var newDimensions = spriteRawExtent * s;
					if (newDimensions.MaxOf <= preferredMaxDimension) {
						scale = s;
						break;
					}
				}
			}

			premultiplyAlpha = premultiplyAlpha && scalerInfo.PremultiplyAlpha;
			gammaCorrect = gammaCorrect && scalerInfo.GammaCorrect; // There is no reason to perform this pass with EPX, as EPX does not blend.

			bool handlePadding = !directImage;

			// Apply padding to the sprite if necessary
			if (handlePadding) {
				spriteRawData = Padding.Apply(
					config: config,
					data: spriteRawData,
					spriteSize: spriteRawExtent,
					scale: scale,
					input: input,
					forcePadding: input.IsFont || input.ForcePadding,
					analysis: analysis,
					padding: out padding,
					paddedSize: out spriteRawExtent
				);
			}

			scaledSize = spriteRawExtent * scale;

			bitmapDataWide = SpanExt.Make<Color16>(scaledSize.Area);

			try {
				var doWrap = wrapped | input.IsWater;

				if (gammaCorrect && currentGammaState == GammaState.Gamma) {
					GammaCorrection.Linearize(spriteRawData, spriteRawExtent);
					currentGammaState = GammaState.Linear;
				}

				if (premultiplyAlpha) {
					PremultipliedAlpha.Reverse(config: config, spriteRawData, spriteRawExtent, fullPremultiplyAlpha);
				}

				if (config.Resample.Deposterization.PreEnabled) {
					spriteRawData = Deposterize.Enhance(config: config, spriteRawData, spriteRawExtent, doWrap);
				}

				var scaler = scalerInfo.Interface;

				var scalerConfig = scaler.CreateConfig(
					config: config,
					wrapped: doWrap,
					hasAlpha: true,
					gammaCorrected: currentGammaState == GammaState.Gamma
				);

				bitmapDataWide = scaler.Apply(
					config: config,
					scalerConfig: scalerConfig,
					scaleMultiplier: scale,
					sourceData: spriteRawData,
					sourceSize: spriteRawExtent,
					targetSize: scaledSize,
					targetData: bitmapDataWide
				);

				if (config.Resample.Deposterization.PostEnabled) {
					bitmapDataWide = Deposterize.Enhance(config: config, bitmapDataWide, scaledSize, doWrap);
				}

				if (config.Resample.UseColorEnhancement) {
					bitmapDataWide = Recolor.Enhance(bitmapDataWide, scaledSize);
				}

				if (premultiplyAlpha) {
					PremultipliedAlpha.Apply(config: config, bitmapDataWide, scaledSize, fullPremultiplyAlpha);
				}

				if (gammaCorrect && currentGammaState == GammaState.Linear) {
					GammaCorrection.Delinearize(bitmapDataWide, scaledSize);
					currentGammaState = GammaState.Gamma;
				}
			}
			catch (Exception ex) {
				Console.Error.WriteLine("Exception: {0}", ex);
				throw;
			}
			//ColorSpace.ConvertLinearToSRGB(bitmapData, Texel.Ordering.ARGB);
		}
		{
			scale = 1;
		}

		if (!padding.IsZero) {
			// Trim excess padding

			// Check initial rows
			// Check ending rows

			// Check initial columns
			// Check ending columns
		}

		{
			var scaledSizeClamped = scaledSize.Min(config.ClampDimension);
			if (scaledSize != scaledSizeClamped) {
				if (scaledSize.Width < scaledSizeClamped.Width || scaledSize.Height < scaledSizeClamped.Height) {
					throw new InvalidOperationException($"Resampled texture size {scaledSize} is smaller than expected {scaledSizeClamped}");
				}

				/* FIXME */// Debug.Trace($"Sprite requires rescaling");
				// This should be incredibly rare - we very rarely need to scale back down.
				// I don't actually have a solution for this case.
				//scaledSizeClamped = scaledSize;
			}
		}

		if (currentGammaState != initialGammaState) {
			throw new InvalidOperationException("Gamma State Mismatch");
		}

		// Narrow
		var bitmapData = Color8.ConvertPinned(bitmapDataWide);

		/*
		if (SMConfig.Debug.Sprite.DumpResample) {
			Textures.DumpTexture(
				source: bitmapData,
				sourceSize: scaledSize,
				//swap: (2, 1, 0, 4),
				path: MakeDumpPath(analysis: analysis, padding: padding, modifiers: new[] { "resample", "narrowed" })
			);
		}
		*/

		Vector2I blockPadding = default;

		// TODO : ref optimize
		if (config.Resample.TrimExcessTransparency) {
			// Detect transparent rows/columns
			(Vector2I Start, Vector2I End) counts = (default, default);

			// Rows

			// From Start
			{
				bool anyTransparent = false;
				for (int y = 0; y < scaledSize.Y; ++y) {
					int offset = scaledSize.X * y;

					bool allTransparent = true;
					for (int x = 0; x < scaledSize.X; ++x) {
						if (bitmapData[offset + x].A != 0) {
							allTransparent = false;
							break;
						}
					}

					if (!allTransparent) {
						counts.Start.Y = y;
						break;
					}

					anyTransparent = true;
				}

				if (anyTransparent && counts.Start.Y == 0) {
					// the entire texture is somehow transparent?
					return ResultInfo.FromFailure(ResampleStatus.DisabledSolid);
				}
			}

			// From End
			{
				bool anyTransparent = false;
				for (int y = scaledSize.Y - 1; y >= 0; --y) {
					int offset = scaledSize.X * y;

					bool allTransparent = true;
					for (int x = 0; x < scaledSize.X; ++x) {
						if (bitmapData[offset + x].A != 0) {
							allTransparent = false;
							break;
						}
					}

					if (!allTransparent) {
						counts.End.Y = y;
						break;
					}

					anyTransparent = true;
				}

				if (anyTransparent && counts.End.Y == 0) {
					// the entire texture is somehow transparent?
					return ResultInfo.FromFailure(ResampleStatus.DisabledSolid);
				}
			}

			// Columns

			// From Start
			{
				bool anyTransparent = false;
				for (int x = 0; x < scaledSize.X; ++x) {
					bool allTransparent = true;
					for (int y = 0; y < scaledSize.Y; ++y) {
						if (bitmapData[(y * scaledSize.X) + x].A != 0) {
							allTransparent = false;
							break;
						}
					}

					if (!allTransparent) {
						counts.Start.X = x;
						break;
					}

					anyTransparent = true;
				}

				if (anyTransparent && counts.Start.X == 0) {
					// the entire texture is somehow transparent?
					return ResultInfo.FromFailure(ResampleStatus.DisabledSolid);
				}
			}

			// From End
			{
				bool anyTransparent = false;
				for (int x = scaledSize.X - 1; x >= 0; --x) {
					bool allTransparent = true;
					for (int y = 0; y < scaledSize.Y; ++y) {
						if (bitmapData[(y * scaledSize.X) + x].A != 0) {
							allTransparent = false;
							break;
						}
					}

					if (!allTransparent) {
						counts.End.X = x;
						break;
					}

					anyTransparent = true;
				}

				if (anyTransparent && counts.End.X == 0) {
					// the entire texture is somehow transparent?
					return ResultInfo.FromFailure(ResampleStatus.DisabledSolid);
				}
			}

			if (!counts.Start.IsZero || !counts.End.IsZero) {
				if (counts.End.X == 0) {
					counts.End.X = scaledSize.X;
				}
				else /*if ((scaledSize.X - counts.Start.X - (scaledSize.X - counts.End.X)) % 4 != 0)*/ {
					counts.End.X++;
				}
				if (counts.End.Y == 0) {
					counts.End.Y = scaledSize.Y;
				}
				else /*if ((scaledSize.Y - counts.Start.Y - (scaledSize.Y - counts.End.Y)) % 4 != 0)*/ {
					counts.End.Y++;
				}

				/*
				if (counts.Start.X != 0) {
					counts.Start.X--;
				}

				if (counts.Start.Y != 0) {
					counts.Start.Y--;
				}
				*/

				Vector2F xPadding = (counts.Start.X, 0);
				Vector2F yPadding = (counts.Start.Y, 0);

				padding.X -= xPadding;
				padding.Y -= yPadding;

				//blockPadding.X = -(scaledSize.X - counts.End.X);
				//blockPadding.Y = -(scaledSize.Y - counts.End.Y);

				padding.X.Y += -(scaledSize.X - counts.End.X);
				padding.Y.Y += -(scaledSize.Y - counts.End.Y);

				if (counts.Start.X == 0 && counts.End.X == scaledSize.X) {
					// If we're only reducing it in height, that makes this far simpler.
					int offset = counts.Start.Y * scaledSize.X;
					int extent = (counts.End.Y - counts.Start.Y) * scaledSize.X;
					bitmapData.Slice(offset, extent).CopyTo(bitmapData.Slice(0, extent));
					bitmapData = bitmapData.Slice(0, extent);
					scaledSize -= (0, counts.Start.Y + (scaledSize.Y - counts.End.Y));
				}
				else {
					// Source and target technically overlap, but there's no contention because we never read somewhere we wrote to. We are always at the same point or ahead.
					int targetWidth = counts.End.X - counts.Start.X;
					int targetHeight = counts.End.Y - counts.Start.Y;

					int sourceStride = scaledSize.X;
					int targetStride = targetWidth;

					int sourceOffset = (counts.Start.Y * sourceStride) + counts.Start.X;
					int targetOffset = 0;

					for (int y = 0; y < targetHeight; ++y) {
						var sourceSlice = bitmapData.Slice(sourceOffset, targetWidth);
						var targetSlice = bitmapData.Slice(targetOffset, targetWidth);
						sourceSlice.CopyTo(targetSlice);
						sourceOffset += sourceStride;
						targetOffset += targetStride;
					}

					bitmapData = bitmapData.Slice(0, targetWidth * targetHeight);
					scaledSize -= (counts.Start.X + (scaledSize.X - counts.End.X), counts.Start.Y + (scaledSize.Y - counts.End.Y));
				}
			}
		}

		var resultData = bitmapData.AsBytes();

		return new() {
			Status = ResampleStatus.Success,
			Data = resultData,
			Wrapped = wrapped,
			Size = scaledSize,
			Padding = padding,
		};
	}
}
