using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using System;
using System.Reflection;
using static SpriteMaster.HarmonyExt.HarmonyExt;
using static SpriteMaster.ScaledTexture;

namespace SpriteMaster.HarmonyExt.Patches.PSpriteBatch {
	class PlatformRenderBatch {
		/*
	public class SpriteBatch : GraphicsResource {
		private struct SpriteInfo {
			public Vector4 Source;

			public Vector4 Destination;

			public Vector2 Origin;

			public float Rotation;

			public float Depth;

			public SpriteEffects Effects;

			public Color Color;
		}
		 */


		private ref struct SpriteInfo {
			private static readonly Type RealType;
			private static readonly FieldInfo SourceInfo;
			private static readonly FieldInfo DestinationInfo;
			private static readonly FieldInfo OriginInfo;
			private static readonly FieldInfo RotationInfo;
			private static readonly FieldInfo DepthInfo;
			private static readonly FieldInfo EffectsInfo;
			private static readonly FieldInfo ColorInfo;

			static SpriteInfo() {
				RealType = typeof(SpriteBatch).GetNestedType("SpriteInfo", BindingFlags.NonPublic);
				FieldInfo GetField(string name) {
					return RealType.GetField(name, BindingFlags.Instance | BindingFlags.Public);
				}
				SourceInfo = GetField("Source");
				DestinationInfo = GetField("Destination");
				OriginInfo = GetField("Origin");
				RotationInfo = GetField("Rotation");
				DepthInfo = GetField("Depth");
				EffectsInfo = GetField("Effects");
				ColorInfo = GetField("Color");
			}

			private readonly object Reference;

			public Vector4 Source {
				readonly get => (Vector4)SourceInfo.GetValue(Reference);
				set => SourceInfo.SetValue(Reference, value);
			}

			public Vector4 Destination {
				readonly get => (Vector4)DestinationInfo.GetValue(Reference);
				set => DestinationInfo.SetValue(Reference, value);
			}

			public Vector2 Origin {
				readonly get => (Vector2)OriginInfo.GetValue(Reference);
				set => OriginInfo.SetValue(Reference, value);
			}

			public float Rotation {
				readonly get => (float)RotationInfo.GetValue(Reference);
				set => RotationInfo.SetValue(Reference, value);
			}

			public float Depth {
				readonly get => (float)DepthInfo.GetValue(Reference);
				set => DepthInfo.SetValue(Reference, value);
			}

			public SpriteEffects Effects {
				readonly get => (SpriteEffects)EffectsInfo.GetValue(Reference);
				set => EffectsInfo.SetValue(Reference, value);
			}

			public Color Color {
				readonly get => (Color)ColorInfo.GetValue(Reference);
				set => ColorInfo.SetValue(Reference, value);
			}

			internal SpriteInfo(object reference) {
				Reference = reference;
			}
		}

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
