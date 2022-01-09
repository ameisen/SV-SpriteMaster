using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SpriteMaster.Config.Resample;


#nullable enable

namespace SpriteMaster.Resample.Passes;

static class Padding {
	private static readonly Color8 padConstant = Color8.Zero;

	private record struct PaddingParameters(Vector2I PaddedSize, Vector2I ActualPadding, Vector2B HasPadding);

	private static bool GetPaddingParameters(in Vector2I spriteSize, uint scale, SpriteInfo input, in Passes.Analysis.LegacyResults analysis, out PaddingParameters parameters) {
		if (!Config.Resample.Padding.Enabled) {
			parameters = new();
			return false;
		}

		var hasPadding = new Vector2B(
			!analysis.Wrapped.X && !analysis.RepeatX.Any && spriteSize.Width > 1,
			!analysis.Wrapped.Y && !analysis.RepeatY.Any && spriteSize.Height > 1
		);

		if (hasPadding.None && (spriteSize.X <= Config.Resample.Padding.MinimumSizeTexels && spriteSize.Y <= Config.Resample.Padding.MinimumSizeTexels)) {
			hasPadding = Vector2B.False;
		}

		if (hasPadding.None && (Config.Resample.Padding.IgnoreUnknown && !input.Reference.Anonymous())) {
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
			HasPadding: hasPadding
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

		// TODO : try to remember why this is * scale * 2. I cannot think of a good reason.
		padding = actualPadding * scale * 2;
		paddedSize = paddedSpriteSize;
		return paddedData;
	}
}
