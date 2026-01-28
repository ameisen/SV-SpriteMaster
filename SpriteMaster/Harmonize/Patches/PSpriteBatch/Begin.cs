using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SpriteMaster.Harmonize.Patches.PSpriteBatch;

internal static class Begin {
	[Harmonize("Begin", fixation: Harmonize.Fixation.Postfix, priority: Harmonize.PriorityLevel.Last)]
	public static void OnBegin(XSpriteBatch __instance, ref SpriteSortMode sortMode, BlendState? blendState, SamplerState? samplerState, DepthStencilState? depthStencilState, RasterizerState? rasterizerState, Effect? effect, Matrix? transformMatrix) {
		if (!SMConfig.IsEnabled) {
			return;
		}

		/*
		if (sortMode is (SpriteSortMode.Deferred or SpriteSortMode.Immediate)) {
			sortMode = SpriteSortMode.Texture;
		}
		*/

		DrawState.OnBegin(
			__instance,
			sortMode,
			blendState ?? BlendState.AlphaBlend,
			samplerState ?? SamplerState.PointClamp,
			depthStencilState ?? DepthStencilState.None,
			rasterizerState ?? RasterizerState.CullCounterClockwise,
			effect,
			transformMatrix ?? Matrix.Identity
		);
	}
}
