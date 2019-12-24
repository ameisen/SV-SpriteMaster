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
					foreach (var i in Config.AbsoluteMaxTextureDimension.To(Config.BaseMaxTextureDimension)) {
						if ((int)maxTextureSizeProperty.GetValue(capabilities) < i) {
							maxTextureSizeProperty.SetValue(capabilities, i);
							getPrivateField(capabilities, "MaxTextureAspectRatio").SetValue(capabilities, i / 2);
							try {
								Config.ClampDimension = i;
								//Math.Min(i, Config.PreferredMaxTextureDimension);
								using (var testTexture = new Texture2D(@this.GraphicsDevice, i, i)) {
									/* do nothing. We want to dispose of it immediately. */
								}
								Garbage.Collect(compact: true, blocking: true, background: false);
								break;
							}
							catch { /* do nothing. resolution unsupported. */ }
							Garbage.Collect(compact: true, blocking: true, background: false);
						}
					}
				}
			}
			catch (Exception ex) {
				ex.PrintWarning();
			}
		}
	}
}
