using SpriteMaster.Types;
using System;

namespace SpriteMaster.Resample.Passes;

static class BlockMultipleAnalysis {
	private static bool BlockTest(ReadOnlySpan<Color8> data, in Bounds textureBounds, in Bounds spriteBounds, int stride, int block) {
		// If it doesn't align to the bounds, then we cannot analyze this multiple.
		if (spriteBounds.Extent % block != Vector2I.Zero) {
			return false;
		}

		// determine reference texel


		var initialOffset = spriteBounds.Top * stride + spriteBounds.Left;
		for (int y = 0; y < spriteBounds.Height; y += block) {
			var yBaseOffset = initialOffset + y * stride;
			for (int x = 0; x < spriteBounds.Width; x += block) {
				for (int subY = 0; subY < block; ++subY) {
					var yOffset = yBaseOffset + (stride * subY);
					var rowReferenceColor = data[yOffset + x];
					for (int subX = 1; subX < block; ++subX) {
						var xOffset = yOffset + x + subX;
						if (!data[xOffset].Equals(rowReferenceColor, Config.Resample.BlockMultipleAnalysis.EqualityThreshold)) {
							return false;
						}
					}
				}
				var columnReferenceColor = data[yBaseOffset];
				// If each row passes, compare the first element of each row against one another
				for (int subY = 1; subY < block; ++subY) {
					var yOffset = yBaseOffset + (stride * subY);
					if (!data[yOffset].Equals(columnReferenceColor, Config.Resample.BlockMultipleAnalysis.EqualityThreshold)) {
						return false;
					}
				}
			}
		}

		return true;
	}

	internal static int Analyze(ReadOnlySpan<Color8> data, in Bounds textureBounds, in Bounds spriteBounds, int stride) {
		// Common multiples
		for (int block = 4; block > 1; --block) {
			if (BlockTest(data, textureBounds, spriteBounds, stride, block)) {
				return block;
			}
		}
		return 1;
	}
}
