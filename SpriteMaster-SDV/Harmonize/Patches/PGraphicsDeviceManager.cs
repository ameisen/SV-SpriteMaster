using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using static SpriteMaster.Harmonize.Harmonize;

namespace SpriteMaster.Harmonize.Patches;

[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Harmony")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Harmony")]
internal static partial class PGraphicsDeviceManager {
	[Harmonize(
		typeof(Game1),
		"SetWindowSize",
		Fixation.Prefix,
		PriorityLevel.Last,
		enabledType: typeof(SMConfig.Debug),
		enabledMember: nameof(SMConfig.Debug.TestZoomedOutOverMax)
	)]
	public static bool OnSetWindowSize(Game1 __instance, ref int w, ref int h) {
		// ReSharper disable HeuristicUnreachableCode
		if (SMConfig.Debug.TestZoomedOutOverMax) {
			Game1.options.desiredBaseZoomLevel = 0.25f;
			Game1.options.baseZoomLevel = 0.25f;
		}
		// ReSharper restore HeuristicUnreachableCode

		return true;
	}

	private static bool ShouldManageRenderTarget(
		ref SurfaceFormat preferredFormat,
		ref DepthFormat preferredDepthFormat,
		ref int preferredMultiSampleCount
	) {
		var stackTrace = new StackTrace(fNeedFileInfo: false);

		foreach (var frame in stackTrace.GetFrames()) {
			var method = frame.GetMethod();
			if (method?.DeclaringType != typeof(StardewValley.Game1)) {
				continue;
			}

			switch (method.Name) {
				case "SetWindowSize":
					if (LastGraphicsDevice is null || !LastGraphicsDevice.TryGetTarget(out var device)) {
						return true;
					}

					preferredMultiSampleCount = (SMConfig.DrawState.AntialiasingSamples > 1) ? SMConfig.DrawState.AntialiasingSamples : 0;
					preferredDepthFormat = device.PresentationParameters.DepthStencilFormat;
					preferredFormat = device.PresentationParameters.BackBufferFormat;
					return true;

				case "Initialize":
				case "allocateLightmap":
				case "takeMapScreenshot":
					return true;
			}
		}

		return false;
	}
}
