using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static SpriteMaster.HarmonyExt.HarmonyExt;

namespace SpriteMaster.HarmonyExt.Patches {
	static class PTexture2D {
		[HarmonyPatch("SetData", HarmonyPatch.Fixation.Postfix, PriorityLevel.Last, HarmonyPatch.Generic.Struct)]
		private static void OnSetData<T> (Texture2D __instance, T[] data) where T : struct {
			ScaledTexture.Purge(__instance);
		}

		[HarmonyPatch("SetData", HarmonyPatch.Fixation.Postfix, PriorityLevel.Last, HarmonyPatch.Generic.Struct)]
		private static void OnSetData<T> (Texture2D __instance, T[] data, int startIndex, int elementCount) where T : struct {
			ScaledTexture.Purge(__instance);
		}

		[HarmonyPatch("SetData", HarmonyPatch.Fixation.Postfix, PriorityLevel.Last, HarmonyPatch.Generic.Struct)]
		private static void OnSetData<T> (Texture2D __instance, int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct {
			ScaledTexture.Purge(__instance, rect ?? null);
		}
	}
}
