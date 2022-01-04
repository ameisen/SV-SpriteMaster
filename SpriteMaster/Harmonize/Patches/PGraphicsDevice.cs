using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using static SpriteMaster.Harmonize.Harmonize;

namespace SpriteMaster.Harmonize.Patches;

[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Harmony")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Harmony")]
static class PGraphicsDevice {
	#region Present

	//[Harmonize("Present", fixation: Harmonize.Fixation.Postfix, priority: PriorityLevel.Last, critical: false)]
	//internal static void PresentPost(GraphicsDevice __instance, in Rectangle? sourceRectangle, in Rectangle? destinationRectangle, IntPtr overrideWindowHandle) => DrawState.OnPresentPost();

	[Harmonize("Present", fixation: Harmonize.Fixation.Prefix, priority: PriorityLevel.Last)]
	internal static bool Present(GraphicsDevice __instance) {
		DrawState.OnPresent();
		return true;
	}

	[Harmonize("Present", fixation: Harmonize.Fixation.Postfix, priority: PriorityLevel.Last)]
	internal static void OnPresent(GraphicsDevice __instance) {
		DrawState.OnPresentPost();
	}

	#endregion

	#region Reset

	private static int ResetReentrancy = 0;

	[Harmonize("Reset", fixation: Harmonize.Fixation.Prefix, priority: PriorityLevel.Last)]
	internal static bool OnResetPre(GraphicsDevice __instance) {
		_ = Interlocked.Increment(ref ResetReentrancy);
		return true;
	}

	[Harmonize("Reset", fixation: Harmonize.Fixation.Postfix, priority: PriorityLevel.Last)]
	internal static void OnResetPost(GraphicsDevice __instance) {
		if (Interlocked.Decrement(ref ResetReentrancy) == 0) {
			DrawState.OnPresentPost();
		}
	}

	[Harmonize("Reset", fixation: Harmonize.Fixation.Prefix, priority: PriorityLevel.Last)]
	internal static bool OnResetPre(GraphicsDevice __instance, PresentationParameters presentationParameters) {
		_ = Interlocked.Increment(ref ResetReentrancy);
		return true;
	}

	[Harmonize("Reset", fixation: Harmonize.Fixation.Postfix, priority: PriorityLevel.Last)]
	internal static void OnResetPost(GraphicsDevice __instance, PresentationParameters presentationParameters) {
		if (Interlocked.Decrement(ref ResetReentrancy) == 0) {
			DrawState.OnPresentPost();
		}
	}

	#endregion
}
