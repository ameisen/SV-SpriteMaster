using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Types;
using static SpriteMaster.HarmonyExt.HarmonyExt;

namespace SpriteMaster.HarmonyExt.Patches {
	static class PTexture2D {
		[HarmonyPatch("SetData", HarmonyPatch.Fixation.Postfix, PriorityLevel.Last, HarmonyPatch.Generic.Struct)]
		private static void OnSetData<T> (Texture2D __instance, T[] data) where T : unmanaged {
			ScaledTexture.Purge(__instance, null, new DataRef<T>(data));
		}

		[HarmonyPatch("SetData", HarmonyPatch.Fixation.Postfix, PriorityLevel.Last, HarmonyPatch.Generic.Struct)]
		private static void OnSetData<T> (Texture2D __instance, T[] data, int startIndex, int elementCount) where T : unmanaged {
			ScaledTexture.Purge(__instance, null, new DataRef<T>(data, startIndex, elementCount));
		}

		[HarmonyPatch("SetData", HarmonyPatch.Fixation.Postfix, PriorityLevel.Last, HarmonyPatch.Generic.Struct)]
		private static void OnSetData<T> (Texture2D __instance, int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : unmanaged {
			ScaledTexture.Purge(__instance, rect, new DataRef<T>(data, startIndex, elementCount));
		}
	}
}
