using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using System;
using System.Reflection;
using static SpriteMaster.HarmonyExt.HarmonyExt;

namespace SpriteMaster.HarmonyExt.Patches {
	static class PGraphicsDeviceManager {

		[HarmonyPatch("ApplyChanges", HarmonyPatch.Fixation.Prefix, PriorityLevel.First)]
		internal static bool OnApplyChanges (GraphicsDeviceManager __instance) {
			var @this = __instance;

			@this.PreferMultiSampling = Config.DrawState.EnableMSAA;
			@this.SynchronizeWithVerticalRetrace = true;
			@this.PreferredBackBufferFormat = Config.DrawState.BackbufferFormat;
			if (Config.DrawState.DisableDepthBuffer)
				@this.PreferredDepthStencilFormat = DepthFormat.None;

			return true;
		}

		[HarmonyPatch("ApplyChanges", HarmonyPatch.Fixation.Postfix, PriorityLevel.Last)]
		internal static void OnApplyChangesPost (GraphicsDeviceManager __instance) {
			var @this = __instance;

			var device = @this.GraphicsDevice;

			try {
				FieldInfo getPrivateField (object obj, string name, bool instance = true) {
					return obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | (instance ? BindingFlags.Instance : BindingFlags.Static));
				}

				var capabilitiesProperty = getPrivateField(device, "_profileCapabilities");

				var capabilitiesMember = capabilitiesProperty.GetValue(device);

				object[] capabilitiesList = new object[] {
					getPrivateField(capabilitiesMember, "HiDef", instance: false).GetValue(capabilitiesMember),
					capabilitiesMember
				};

				foreach (var capabilities in capabilitiesList) {
					if (capabilities == null) {
						continue;
					}
					var maxTextureSizeProperty = getPrivateField(capabilities, "MaxTextureSize");
					if ((int)maxTextureSizeProperty.GetValue(capabilities) < Config.PreferredMaxTextureDimension) {
						maxTextureSizeProperty.SetValue(capabilities, Config.PreferredMaxTextureDimension);
						getPrivateField(capabilities, "MaxTextureAspectRatio").SetValue(capabilities, Config.PreferredMaxTextureDimension / 2);
						Config.ClampDimension = Config.PreferredMaxTextureDimension;
					}
				}
			}
			catch (Exception ex) {
				ex.PrintWarning();
			}
		}
	}
}
