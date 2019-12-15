using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using System.Linq;
using System.Threading;
using System;
using System.Collections.Generic;

namespace SpriteMaster
{
	// Modified from PyTK.Overrides.OvSpritebatchNew
	// Origial Source: https://github.com/Platonymous/Stardew-Valley-Mods/blob/master/PyTK/Overrides/OvSpritebatchNew.cs
	// Original Licence: GNU General Public License v3.0
	// Original Author: Platonymous

	static class Extension
	{
		internal static int ToCoordinate(this float coordinate)
		{
			return coordinate.RoundToInt();
		}

		internal static int ToCoordinate(this double coordinate)
		{
			return coordinate.RoundToInt();
		}
	}

	internal sealed class Patches
	{
		internal static readonly ThreadLocal<bool> ReentranceLock = new ThreadLocal<bool>(false);

		private partial class Harmony
		{
			private static Type[] GetMethodParameters(in MethodInfo method)
			{
				var filteredParameters = method.GetParameters().Where(t => !t.Name.Equals("__instance"));
				return filteredParameters.Select(p => p.ParameterType).ToArray();
			}

			internal static void Patch(in HarmonyInstance instance, in Type type, string name, in MethodInfo method)
			{
				var methodParameters = GetMethodParameters(method);
				var typeMethod = type.GetMethod(name, methodParameters);
				if (typeMethod == null)
				{
					var typeMethods = type.GetMethods().Where(t => t.Name == name);
					foreach (var testMethod in typeMethods)
					{
						// Compare the parameters. Ignore references.
						var testParameters = testMethod.GetParameters();
						if (testParameters.Length != methodParameters.Length)
						{
							continue;
						}

						bool found = true;
						foreach (var i in 0.Until(testParameters.Length))
						{
							Type AsReference(in Type type)
							{
								return type.IsByRef ? type : type.MakeByRefType();
							}

							var testParameter = AsReference(testParameters[i].ParameterType);
							var methodParameter = AsReference(methodParameters[i]);
							if (!testParameter.Equals(methodParameter))
							{
								found = false;
								break;
							}
						}
						if (found)
						{
							typeMethod = testMethod;
							break;
						}
					}

					if (typeMethod == null)
					{
						Debug.ErrorLn($"Failed to patch {type.Name}.{name}");
						return;
					}
				}
				var harmonyMethod = new HarmonyMethod(method);
				harmonyMethod.prioritiy = Priority.Last; // sic
				instance.Patch(
					typeMethod,
					harmonyMethod,
					null,
					null
				);
			}
		}

		internal static void InitializePatch(in HarmonyInstance instance)
		{
			IEnumerable<MethodInfo> GetMethods<T>(string name)
			{
				return typeof(T).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(m => m.Name == name);
			}

			foreach (MethodInfo method in GetMethods<Patches.Harmony>("Draw"))
				Harmony.Patch(instance, typeof(SpriteBatch), "Draw", method);

			foreach (MethodInfo method in GetMethods<Patches>("Begin"))
				Harmony.Patch(instance, typeof(SpriteBatch), "Begin", method);

			foreach (MethodInfo method in GetMethods<Patches>("ApplyChanges"))
				Harmony.Patch(instance, typeof(GraphicsDeviceManager), "ApplyChanges", method);

			foreach (MethodInfo method in GetMethods<Patches>("Present"))
				Harmony.Patch(instance, typeof(GraphicsDevice), "Present", method);

			// https://github.com/pardeike/Harmony/issues/121
			//foreach (MethodInfo method in typeof(DrawPatched).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(m => m.Name == "SetData"))
			//	Harmony.Patch(instance, typeof(Texture2D), "SetData", method);
		}

		internal static bool ApplyChanges(GraphicsDeviceManager __instance)
		{
			__instance.PreferMultiSampling = true;
			__instance.GraphicsDevice.PresentationParameters.MultiSampleCount = 8;
			return true;
		}

		private static bool FetchedThisFrame = false;
		private static int RemainingTexelFetchBudget = Config.AsyncScaling.TexelFetchFrameBudget;
		private static bool PushedUpdateThisFrame = false;
		public static TextureAddressMode CurrentAddressModeU = TextureAddressMode.Clamp;
		public static TextureAddressMode CurrentAddressModeV = TextureAddressMode.Clamp;
		public static Blend CurrentBlendSourceMode = BlendState.AlphaBlend.AlphaSourceBlend;

		internal static bool SetData<T>(in Texture2D __instance, T[] data) where T : struct
		{
			return true;
		}
		internal static bool SetData<T>(in Texture2D __instance, T[] data, int startIndex, int elementCount) where T : struct
		{
			return true;
		}
		internal static bool SetData<T>(in Texture2D __instance, int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct
		{
			return true;
		}

		private static void SetCurrentAddressMode(in SamplerState samplerState)
		{
			CurrentAddressModeU = samplerState.AddressU;
			CurrentAddressModeV = samplerState.AddressV;
		}

		internal static bool GetUpdateToken(int texels)
		{
			if (FetchedThisFrame && texels > RemainingTexelFetchBudget)
			{
				return false;
			}

			FetchedThisFrame = true;
			RemainingTexelFetchBudget -= texels;
			return true;
		}

		private static void OnPresent()
		{
			if (Config.AsyncScaling.CanFetchAndLoadSameFrame || !PushedUpdateThisFrame)
			{
				ScaledTexture.ProcessPendingActions(100);
			}
			RemainingTexelFetchBudget = Config.AsyncScaling.TexelFetchFrameBudget;
			FetchedThisFrame = false;
			PushedUpdateThisFrame = false;
		}

		internal static bool Present()
		{
			OnPresent();
			return true;
		}

		internal static bool Present(Rectangle? sourceRectangle, Rectangle? destinationRectangle, IntPtr overrideWindowHandle)
		{
			OnPresent();
			return true;
		}

		private static bool OnBegin(
			SpriteBatch __instance,
			SpriteSortMode sortMode = SpriteSortMode.Deferred,
			BlendState blendState = null,
			SamplerState samplerState = null,
			DepthStencilState depthStencilState = null,
			RasterizerState rasterizerState = null,
			Effect effect = null,
			Matrix? transformMatrixRef = null
		)
		{
			blendState ??= BlendState.AlphaBlend;
			samplerState ??= SamplerState.LinearClamp;
			depthStencilState ??= DepthStencilState.Default;
			rasterizerState ??= RasterizerState.CullCounterClockwise;
			var transformMatrix = (transformMatrixRef != null) ? transformMatrixRef.GetValueOrDefault(Matrix.Identity) : Matrix.Identity;

			SetCurrentAddressMode(samplerState);
			CurrentBlendSourceMode = blendState.AlphaSourceBlend;
			return true;
		}

		internal static bool Begin(SpriteBatch __instance)
		{
			return OnBegin(__instance);
		}
		internal static bool Begin(SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState)
		{
			return OnBegin(__instance, sortMode, blendState);
		}
		internal static bool Begin(SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState, ref SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState)
		{
			return OnBegin(__instance, sortMode, blendState, samplerState, depthStencilState, rasterizerState);
		}
		internal static bool Begin(SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState, ref SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect)
		{
			return OnBegin(__instance, sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect);
		}
		internal static bool Begin(SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState, ref SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix transformMatrix)
		{
			return OnBegin(__instance, sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix);
		}

		private sealed class AddressModeHandler : IDisposable
		{
			private const bool Enabled = Config.Resample.EnableWrappedAddressing;
			private readonly SamplerState OriginalState = SamplerState.PointClamp;
			private readonly SpriteBatch Batch = null;

			internal AddressModeHandler(in SpriteBatch spriteBatch, in ScaledTexture texture)
			{
				if (!Enabled)
				{
					return;
				}

				Batch = spriteBatch;

				OriginalState = spriteBatch.GraphicsDevice.SamplerStates[0];
				var newState = new SamplerState()
				{
					AddressU = texture.Wrapped.X ? TextureAddressMode.Wrap : OriginalState.AddressU,
					AddressV = texture.Wrapped.Y ? TextureAddressMode.Wrap : OriginalState.AddressV,
					AddressW = OriginalState.AddressW,
					MaxAnisotropy = OriginalState.MaxAnisotropy,
					MaxMipLevel = OriginalState.MaxMipLevel,
					MipMapLevelOfDetailBias = OriginalState.MipMapLevelOfDetailBias,
					Name = "RescaledSampler",
					Tag = OriginalState.Tag,
					Filter = Config.DrawState.SetLinear ? TextureFilter.Linear : OriginalState.Filter
				};

				spriteBatch.GraphicsDevice.SamplerStates[0] = newState;
			}

			public void Dispose()
			{
				if (!Enabled)
				{
					return;
				}

				Batch.GraphicsDevice.SamplerStates[0] = OriginalState;
			}
		}

		private sealed class ReentranceLockWrapper : IDisposable
		{
			private readonly ThreadLocal<bool> ReentranceLock;

			internal ReentranceLockWrapper(in ThreadLocal<bool> reentranceLock)
			{
				this.ReentranceLock = reentranceLock;
				ReentranceLock.Value = true;
			}

			public void Dispose()
			{
				ReentranceLock.Value = false;
			}
		}

		private static ScaledTexture DrawHandler(
			in SpriteBatch __instance,
			Texture2D texture,
			ref Rectangle sourceRectangle
		)
		{
			try
			{
				if (Config.ClampInvalidBounds)
				{
					sourceRectangle = sourceRectangle.ClampTo(new Rectangle(0, 0, texture.Width, texture.Height));
				}

				// Let's just skip potentially invalid draws since I have no idea what to do with them.
				if (sourceRectangle.Height <= 0 || sourceRectangle.Width <= 0)
				{
					return null;
				}

				ScaledTexture scaledTexture = null;
				// Load textures on the fly.
				if (!(texture is RenderTarget2D) && texture.Width >= 1 && texture.Height >= 1)
				{
					scaledTexture = ScaledTexture.Get(texture, sourceRectangle);
				}

				if (scaledTexture != null && scaledTexture.IsReady)
				{
					Texture2D t = scaledTexture.Texture;

					if (t == null) return null;

					if (Config.Resample.DeSprite && scaledTexture.IsSprite)
					{
						sourceRectangle = new Rectangle(
							0,
							0,
							t.Width,
							t.Height
						);
					}
					else
					{
						sourceRectangle = new Rectangle(
							(sourceRectangle.X * scaledTexture.Scale.X).ToCoordinate(),
							(sourceRectangle.Y * scaledTexture.Scale.Y).ToCoordinate(),
							(sourceRectangle.Width * scaledTexture.Scale.X).ToCoordinate(),
							(sourceRectangle.Height * scaledTexture.Scale.Y).ToCoordinate()
						);
					}

					return scaledTexture;
				}
			}
			catch (Exception ex)
			{
				Debug.ErrorLn($"Exception In DrawHandler: {ex.Message}");
				Debug.ErrorLn(ex.GetStackTrace());
			}

			return null;
		}

		internal static bool DrawPatched(
			in SpriteBatch __instance,
			in Texture2D texture,
			in Rectangle destination,
			in Rectangle? sourceRectangleRef,
			in Color color,
			in Vector2 origin,
			in float rotation = 0f,
			in SpriteEffects effects = SpriteEffects.None,
			in float layerDepth = 0f
		)
		{
			if (!Config.Enabled || ReentranceLock.Value) return true;
			var sourceRectangle = (sourceRectangleRef == null || !sourceRectangleRef.HasValue) ? new Rectangle(0, 0, texture.Width, texture.Height) : sourceRectangleRef.Value;

			var scaledTexture = DrawHandler(__instance, texture, ref sourceRectangle);
			if (scaledTexture == null)
			{
				return true;
			}

			Texture2D t = scaledTexture.Texture;

			try
			{
				using (new ReentranceLockWrapper(ReentranceLock))
				{
					using (new AddressModeHandler(__instance, scaledTexture))
					{
						__instance.Draw(
							t,
							destination,
							sourceRectangle,
							color,
							rotation,
							origin * scaledTexture.Scale,
							effects,
							layerDepth
						);
					}
				}
			}
			catch
			{
				return true;
			}

			return false;
		}

		internal static bool DrawPatched(
			in SpriteBatch __instance,
			in Texture2D texture,
			in Vector2 position,
			in Rectangle? sourceRectangleRef,
			in Color color,
			in Vector2 origin,
			in Vector2 scale,
			in float rotation = 0f,
			in SpriteEffects effects = SpriteEffects.None,
			in float layerDepth = 0f
		)
		{
			if (!Config.Enabled || ReentranceLock.Value) return true;
			var sourceRectangle = (sourceRectangleRef == null || !sourceRectangleRef.HasValue) ? new Rectangle(0, 0, texture.Width, texture.Height) : sourceRectangleRef.Value;

			var scaledTexture = DrawHandler(__instance, texture, ref sourceRectangle);
			if (scaledTexture == null)
			{
				return true;
			}

			Texture2D t = scaledTexture.Texture;

			try
			{
				// The math here is certainly more fun since we're working with an offset and a scale rather than just a rectangle!
				// The position should stay unchanged, but the scale needs to be adjusted by the inverse of the scaled... scale
				var scaledScale = scale / scaledTexture.Scale;

				using (new ReentranceLockWrapper(ReentranceLock))
				{
					using (new AddressModeHandler(__instance, scaledTexture))
					{
						__instance.Draw(
							t,
							position,
							sourceRectangle,
							color,
							rotation,
							origin * scaledTexture.Scale,
							scaledScale,
							effects,
							layerDepth
						);
					}
				}
			}
			catch
			{
				return true;
			}

			return false;
		}

		private partial class Harmony
		{
			internal static bool Draw(SpriteBatch __instance, Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, SpriteEffects effects, float layerDepth)
			{
				return DrawPatched(__instance, texture, destinationRectangle, sourceRectangle, color, origin, rotation, effects, layerDepth);
			}
			internal static bool Draw(SpriteBatch __instance, Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color)
			{
				return DrawPatched(__instance, texture, destinationRectangle, sourceRectangle, color, Vector2.Zero);
			}
			internal static bool Draw(SpriteBatch __instance, Texture2D texture, Rectangle destinationRectangle, Color color)
			{
				return DrawPatched(__instance, texture, destinationRectangle, null, color, Vector2.Zero);
			}
			internal static bool Draw(SpriteBatch __instance, Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
			{
				return DrawPatched(__instance, texture, position, sourceRectangle, color, origin, scale, rotation, effects, layerDepth);
			}
			internal static bool Draw(SpriteBatch __instance, Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
			{
				return DrawPatched(__instance, texture, position, sourceRectangle, color, origin, new Vector2(scale, scale), rotation, effects, layerDepth);
			}
			internal static bool Draw(SpriteBatch __instance, Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color)
			{
				return DrawPatched(__instance, texture, position, sourceRectangle, color, Vector2.Zero, Vector2.One);
			}
			internal static bool Draw(SpriteBatch __instance, Texture2D texture, Vector2 position, Color color)
			{
				return DrawPatched(__instance, texture, position, null, color, Vector2.Zero, Vector2.One);
			}
		}
	}
}
