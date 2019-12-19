using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace SpriteMaster.HarmonyExt.Patches {
	static class PGraphicsDevice {
		[HarmonyPatch("Present")]
		internal static bool Present (GraphicsDevice __instance) {
			DrawState.OnPresent();
			return true;
		}

		[HarmonyPatch("Present")]
		internal static bool Present (GraphicsDevice __instance, Rectangle? sourceRectangle, Rectangle? destinationRectangle, IntPtr overrideWindowHandle) {
			DrawState.OnPresent();
			return true;
		}
	}
}
