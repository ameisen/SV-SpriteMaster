using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;

#nullable enable

namespace SpriteMaster.Resample.Passes;

static class Padding {
	private static readonly Color8 padConstant = Color8.Zero;

	private record struct PaddingParameters(Vector2I PaddedSize, Vector2I ActualPadding, Vector2B HasPadding, Vector2B SolidEdge);

	private static bool GetPaddingParameters(in Vector2I spriteSize, uint scale, SpriteInfo input, in Passes.Analysis.LegacyResults analysis, out PaddingParameters parameters) {
		if (!Config.Resample.Padding.Enabled) {
			parameters = new();
			return false;
		}

		var hasPadding = Vector2B.True;

		Vector2B solidEdge = (
			analysis.Wrapped.X || analysis.RepeatX.Any || spriteSize.Width <= 1,
			analysis.Wrapped.Y || analysis.RepeatY.Any || spriteSize.Height <= 1
		);

		if (!Config.Resample.Padding.PadSolidEdges) {
			hasPadding = solidEdge.Invert;
		}

		if (hasPadding.Any && (spriteSize.X <= Config.Resample.Padding.MinimumSizeTexels && spriteSize.Y <= Config.Resample.Padding.MinimumSizeTexels)) {
			hasPadding = Vector2B.False;
		}

		if (hasPadding.Any && (Config.Resample.Padding.IgnoreUnknown && !input.Reference.Anonymous())) {
			hasPadding = Vector2B.False;
		}

		if (hasPadding.None) {
			parameters = new();
			return false;
		}

		var expectedPadding = Math.Max(1U, scale / 2);
		var expectedPaddingBoth = expectedPadding * 2;

		// TODO we only need to pad the edge that has texels. Double padding is wasteful.
		var paddedSpriteSize = spriteSize;

		var actualPadding = Vector2I.Zero;

		if (hasPadding.X) {
			if ((paddedSpriteSize.X + expectedPaddingBoth) * scale > Config.ClampDimension) {
				hasPadding.X = false;
			}
			else {
				paddedSpriteSize.X += (int)expectedPaddingBoth;
				actualPadding.X = (int)expectedPadding;
			}
		}
		if (hasPadding.Y) {
			if ((paddedSpriteSize.Y + expectedPaddingBoth) * scale > Config.ClampDimension) {
				hasPadding.Y = false;
			}
			else {
				paddedSpriteSize.Y += (int)expectedPaddingBoth;
				actualPadding.Y = (int)expectedPadding;
			}
		}

		if (hasPadding.None) {
			parameters = new();
			return false;
		}

		parameters = new(
			PaddedSize: paddedSpriteSize,
			ActualPadding: actualPadding,
			HasPadding: hasPadding,
			SolidEdge: solidEdge
		);

		return true;
	}

	internal static Span<Color8> Apply(ReadOnlySpan<Color8> data, in Vector2I spriteSize, uint scale, SpriteInfo input, in Passes.Analysis.LegacyResults analysis, out Vector2I padding, out Vector2I paddedSize) {
		if (!GetPaddingParameters(spriteSize, scale, input, analysis, out var parameters)) {
			padding = Vector2I.Zero;
			paddedSize = spriteSize;
			return data.ToSpanUnsafe();
		}

		var paddedSpriteSize = parameters.PaddedSize;
		var actualPadding = parameters.ActualPadding;
		var hasPadding = parameters.HasPadding;

		// The actual padding logic. If we get to this point, we are actually performing padding.

		var paddedData = SpanExt.MakeUninitialized<Color8>(paddedSpriteSize.Area);

		{
			int y = 0;

			void WritePaddingY(Span<Color8> data) {
				if (!hasPadding.Y) {
					return;
				}

				for (int i = 0; i < actualPadding.Y; ++i) {
					var strideOffset = y * paddedSpriteSize.Width;
					for (int x = 0; x < paddedSpriteSize.Width; ++x) {
						data[strideOffset + x] = padConstant;
					}
					++y;
				}
			}

			WritePaddingY(paddedData);

			void WritePaddingX(Span<Color8> data, ref int xOffset) {
				if (!hasPadding.X) {
					return;
				}

				for (int x = 0; x < actualPadding.X; ++x) {
					data[xOffset++] = padConstant;
				}
			}

			for (int i = 0; i < spriteSize.Height; ++i) {
				// Write a padded X line
				var xOffset = y * paddedSpriteSize.Width;

				WritePaddingX(paddedData, ref xOffset);
				data.CopyTo(paddedData, i * spriteSize.Width, xOffset, spriteSize.Width);
				xOffset += spriteSize.Width;
				WritePaddingX(paddedData, ref xOffset);
				++y;
			}

			WritePaddingY(paddedData);
		}

		// If we had solid edges that we are padding, copy the color (but not the alpha) over by one.
		if (parameters.HasPadding.X && parameters.SolidEdge.X) {
			for (int y = 0; y < spriteSize.Height; ++y) {
				int yOffset = (y + actualPadding.Y) * paddedSpriteSize.Width;

				int xSrcOffset0 = actualPadding.X;
				int xSrcOffset1 = xSrcOffset0 + spriteSize.Width - 1;

				var src = paddedData[yOffset + xSrcOffset0];
				src.A = 128;
				paddedData[yOffset + xSrcOffset0 - 1] = src;
				src = paddedData[yOffset + xSrcOffset1];
				src.A = 128;
				paddedData[yOffset + xSrcOffset1 + 1] = src;
			}
		}
		if (parameters.HasPadding.Y && parameters.SolidEdge.Y) {
			bool withXPadding = parameters.HasPadding.X && parameters.SolidEdge.X;

			int ySrcOffset0 = actualPadding.Y * paddedSpriteSize.Width;
			int yDstOffset0 = (actualPadding.Y - 1) * paddedSpriteSize.Width;
			int ySrcOffset1 = (actualPadding.Y + spriteSize.Y - 1) * paddedSpriteSize.Width;
			int yDstOffset1 = (actualPadding.Y + spriteSize.Y) * paddedSpriteSize.Width;

			int xOffset = withXPadding ? -1 : 0;
			int widthAdd = withXPadding ? 2 : 0;

			for (int x = 0; x < spriteSize.Width + widthAdd; ++x) {
				var src = paddedData[ySrcOffset0 + actualPadding.X + xOffset + x];
				src.A = 128;
				paddedData[yDstOffset0 + actualPadding.X + xOffset + x] = src;
			}
			for (int x = 0; x < spriteSize.Width + widthAdd; ++x) {
				var src = paddedData[ySrcOffset1 + actualPadding.X + xOffset + x];
				src.A = 128;
				paddedData[yDstOffset1 + actualPadding.X + xOffset + x] = src;
			}
		}

		// TODO : try to remember why this is * scale * 2. I cannot think of a good reason.
		padding = actualPadding * scale * 2;
		paddedSize = paddedSpriteSize;
		return paddedData;
	}
}
