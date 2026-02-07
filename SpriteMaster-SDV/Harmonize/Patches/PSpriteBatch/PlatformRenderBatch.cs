using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Harmonize.Patches.PSpriteBatch;

[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Harmony")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Harmony")]
internal static partial class PlatformRenderBatch {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int? GetMaxBatchSize(SpriteBatcher batcher) {
		if (Game1.spriteBatch is null || ReferenceEquals(batcher, Game1.spriteBatch._batcher)) {
			return SpriteBatcher.MaxBatchSize;
		}

		return null;
	}
}
