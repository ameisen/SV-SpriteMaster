using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.InteropServices;
using static SpriteMaster.HarmonyExt.HarmonyExt;

namespace SpriteMaster.HarmonyExt.Patches {
	internal static class PRenderTarget2D {
		[HarmonyPatch("CreateRenderTarget", HarmonyPatch.Fixation.Prefix, PriorityLevel.Last)]
		private static bool CreateRenderTarget (RenderTarget2D __instance, GraphicsDevice graphicsDevice, ref int width, ref int height, [MarshalAs(UnmanagedType.U1)] ref bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, ref int preferredMultiSampleCount, RenderTargetUsage usage) {
			if (width >= graphicsDevice.Viewport.Width && height >= graphicsDevice.Viewport.Height) {
				width = Math.Min(Config.AbsoluteMaxTextureDimension, width * 2);
				height = Math.Min(Config.AbsoluteMaxTextureDimension, height * 2);
				preferredMultiSampleCount = Config.DrawState.EnableMSAA ? Math.Max(2, preferredMultiSampleCount) : preferredMultiSampleCount;
			}
			/*
			else {
				width = Math.Max(graphicsDevice.Viewport.Width * 2, width * 2);
				height = Math.Max(graphicsDevice.Viewport.Height * 2, height * 2);
				preferredMultiSampleCount = Config.DrawState.EnableMSAA ? Math.Max(2, preferredMultiSampleCount) : preferredMultiSampleCount;
			}
			*/
			// This is required to prevent aliasing effects.
			mipMap = true;
			return true;
		}
	}
}
