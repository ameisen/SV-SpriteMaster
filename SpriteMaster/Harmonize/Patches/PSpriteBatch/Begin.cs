using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Harmonize.Patches.PSpriteBatch;

static class Begin {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	[Harmonize("Begin", fixation: Harmonize.Fixation.Postfix, priority: Harmonize.PriorityLevel.Last)]
	internal static void OnBegin(SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix? transformMatrix) {
		if (!Config.IsEnabled) {
			return;
		}

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
