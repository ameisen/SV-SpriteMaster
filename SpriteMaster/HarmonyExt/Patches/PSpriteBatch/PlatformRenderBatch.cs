using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using System;
using static SpriteMaster.HarmonyExt.HarmonyExt;
using static SpriteMaster.ScaledTexture;

namespace SpriteMaster.HarmonyExt.Patches.PSpriteBatch {
	class PlatformRenderBatch {
		[HarmonyPatch("PlatformRenderBatch", HarmonyPatch.Fixation.Prefix, PriorityLevel.First)]
		internal static bool OnPlatformRenderBatch (
			SpriteBatch __instance,
			Texture2D texture,
			object[] sprites,
			int offset,
			int count,
			ref SamplerState ___samplerState,
			ref SamplerState __state
		) {
			try {
				var OriginalState = ___samplerState;
				__state = OriginalState;

				if (texture is ManagedTexture2D managedTexture) {
					var newState = new SamplerState() {
						AddressU = managedTexture.Texture.Wrapped.X ? TextureAddressMode.Wrap : OriginalState.AddressU,
						AddressV = managedTexture.Texture.Wrapped.Y ? TextureAddressMode.Wrap : OriginalState.AddressV,
						AddressW = OriginalState.AddressW,
						MaxAnisotropy = OriginalState.MaxAnisotropy,
						MaxMipLevel = OriginalState.MaxMipLevel,
						MipMapLevelOfDetailBias = OriginalState.MipMapLevelOfDetailBias,
						Name = "RescaledSampler",
						Tag = OriginalState.Tag,
						Filter = (Config.DrawState.SetLinear) ? TextureFilter.Linear : OriginalState.Filter
					};

					__instance.GraphicsDevice.SamplerStates[0] = newState;
					___samplerState = newState;
				}
				else if (texture is RenderTarget2D) {
					var newState = new SamplerState() {
						AddressU = OriginalState.AddressU,
						AddressV = OriginalState.AddressV,
						AddressW = OriginalState.AddressW,
						MaxAnisotropy = OriginalState.MaxAnisotropy,
						MaxMipLevel = OriginalState.MaxMipLevel,
						MipMapLevelOfDetailBias = OriginalState.MipMapLevelOfDetailBias,
						Name = "RescaledSampler",
						Tag = OriginalState.Tag,
						Filter = (Config.DrawState.SetLinear) ? TextureFilter.Linear : OriginalState.Filter
					};

					__instance.GraphicsDevice.SamplerStates[0] = newState;
					___samplerState = newState;
				}
			}
			catch (Exception ex) {
				ex.PrintError();
			}

			return true;
		}

		[HarmonyPatch("PlatformRenderBatch", HarmonyPatch.Fixation.Postfix, PriorityLevel.Last)]
		internal static void OnPlatformRenderBatchPost (
			SpriteBatch __instance,
			Texture2D texture,
			object[] sprites,
			int offset,
			int count,
			ref SamplerState ___samplerState,
			ref SamplerState __state
		) {
			try {
				__instance.GraphicsDevice.SamplerStates[0] = __state;
				___samplerState = __state;
			}
			catch (Exception ex) {
				ex.PrintError();
			}
		}
	}
}
