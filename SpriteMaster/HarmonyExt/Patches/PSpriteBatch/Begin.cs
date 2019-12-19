using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static SpriteMaster.HarmonyExt.HarmonyExt;

namespace SpriteMaster.HarmonyExt.Patches.PSpriteBatch {
	static class Begin {
		[HarmonyPatch("Begin", priority: PriorityLevel.First)]
		internal static bool OnBegin (SpriteBatch __instance) {
			__instance.Begin(
				SpriteSortMode.Deferred,
				BlendState.AlphaBlend,
				SamplerState.LinearClamp,
				DepthStencilState.Default,
				RasterizerState.CullCounterClockwise,
				null,
				Matrix.Identity
			);
			return false;
		}
		[HarmonyPatch("Begin", priority: PriorityLevel.First)]
		internal static bool OnBegin (SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState) {
			__instance.Begin(
				sortMode,
				blendState,
				SamplerState.LinearClamp,
				DepthStencilState.Default,
				RasterizerState.CullCounterClockwise,
				null,
				Matrix.Identity
			);
			return false;
		}
		[HarmonyPatch("Begin", priority: PriorityLevel.First)]
		internal static bool OnBegin (SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState) {
			__instance.Begin(
				sortMode,
				blendState,
				samplerState,
				depthStencilState,
				rasterizerState,
				null,
				Matrix.Identity
			);
			return false;
		}
		[HarmonyPatch("Begin", priority: PriorityLevel.First)]
		internal static bool OnBegin (SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect) {
			__instance.Begin(
				sortMode,
				blendState,
				samplerState,
				depthStencilState,
				rasterizerState,
				effect,
				Matrix.Identity
			);
			return false;
		}

		[HarmonyPatch("Begin")]
		internal static bool OnBegin (SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix transformMatrix) {
			DrawState.OnBegin(__instance, sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix);
			return true;
		}
	}
}
