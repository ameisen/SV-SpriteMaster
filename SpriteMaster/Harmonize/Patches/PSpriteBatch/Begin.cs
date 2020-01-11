using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static SpriteMaster.Harmonize.Harmonize;

namespace SpriteMaster.Harmonize.Patches.PSpriteBatch {
	static class Begin {
		[Harmonize("Begin", fixation: AffixType.Postfix, priority: PriorityLevel.Last)]
		internal static void OnBegin (SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix transformMatrix) {
			DrawState.OnBegin(
				__instance,
				sortMode,
				blendState ?? BlendState.AlphaBlend,
				samplerState ?? SamplerState.PointClamp,
				depthStencilState ?? DepthStencilState.None,
				rasterizerState ?? RasterizerState.CullCounterClockwise,
				effect,
				transformMatrix
			);
		}
	}
}
