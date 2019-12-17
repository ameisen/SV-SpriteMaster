using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using System.Linq;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SpriteMaster {
	static class Extension {
		internal static int ToCoordinate (this float coordinate) {
			return coordinate.RoundToInt();
		}

		internal static int ToCoordinate (this double coordinate) {
			return coordinate.RoundToInt();
		}
	}

	internal sealed class Patches {
		private partial class Harmony {
			private static Type[] GetMethodParameters (in MethodInfo method) {
				var filteredParameters = method.GetParameters().Where(t => !t.Name.Equals("__instance"));
				return filteredParameters.Select(p => p.ParameterType).ToArray();
			}

			internal static void Patch<T> (in HarmonyInstance instance, string name, in MethodInfo method) {
				var type = typeof(T);
				var methodParameters = GetMethodParameters(method);
				var typeMethod = type.GetMethod(name, methodParameters);
				if (typeMethod == null) {
					var typeMethods = type.GetMethods().Where(t => t.Name == name);
					foreach (var testMethod in typeMethods) {
						// Compare the parameters. Ignore references.
						var testParameters = testMethod.GetParameters();
						if (testParameters.Length != methodParameters.Length) {
							continue;
						}

						bool found = true;
						foreach (var i in 0.Until(testParameters.Length)) {
							Type AsReference (in Type type) {
								return type.IsByRef ? type : type.MakeByRefType();
							}

							var testParameter = AsReference(testParameters[i].ParameterType);
							var methodParameter = AsReference(methodParameters[i]);
							if (!testParameter.Equals(methodParameter)) {
								found = false;
								break;
							}
						}
						if (found) {
							typeMethod = testMethod;
							break;
						}
					}

					if (typeMethod == null) {
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

		internal static void InitializePatch (in HarmonyInstance instance) {
			IEnumerable<MethodInfo> GetMethods<T> (string name) {
				return typeof(T).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(m => m.Name == name);
			}

			foreach (var method in GetMethods<Patches.Harmony>("Draw"))
				Harmony.Patch<SpriteBatch>(instance, "Draw", method);

			foreach (var method in GetMethods<Patches>("Begin"))
				Harmony.Patch<SpriteBatch>(instance, "Begin", method);

			foreach (var method in GetMethods<Patches>("ApplyChanges"))
				Harmony.Patch<GraphicsDeviceManager>(instance, "ApplyChanges", method);

			foreach (var method in GetMethods<Patches>("Present"))
				Harmony.Patch<GraphicsDevice>(instance, "Present", method);

			// https://github.com/pardeike/Harmony/issues/121
			//foreach (MethodInfo method in typeof(DrawPatched).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(m => m.Name == "SetData"))
			//	Harmony.Patch(instance, typeof(Texture2D), "SetData", method);
		}

		internal static bool ApplyChanges (GraphicsDeviceManager __instance) {
			__instance.PreferMultiSampling = true;
			__instance.GraphicsDevice.PresentationParameters.MultiSampleCount = 8;
			return true;
		}

		private static readonly SamplerState DefaultSamplerState = SamplerState.LinearClamp;
		private static bool FetchedThisFrame = false;
		private static int RemainingTexelFetchBudget = Config.AsyncScaling.TexelFetchFrameBudget;
		private static bool PushedUpdateThisFrame = false;
		public static TextureAddressMode CurrentAddressModeU = DefaultSamplerState.AddressU;
		public static TextureAddressMode CurrentAddressModeV = DefaultSamplerState.AddressV;
		public static Blend CurrentBlendSourceMode = BlendState.AlphaBlend.AlphaSourceBlend;

		internal static bool SetData<T> (in Texture2D __instance, T[] data) where T : struct {
			return true;
		}
		internal static bool SetData<T> (in Texture2D __instance, T[] data, int startIndex, int elementCount) where T : struct {
			return true;
		}
		internal static bool SetData<T> (in Texture2D __instance, int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct {
			return true;
		}

		private static void SetCurrentAddressMode (in SamplerState samplerState) {
			CurrentAddressModeU = samplerState.AddressU;
			CurrentAddressModeV = samplerState.AddressV;
		}

		internal static bool GetUpdateToken (int texels) {
			if (FetchedThisFrame && texels > RemainingTexelFetchBudget) {
				return false;
			}

			FetchedThisFrame = true;
			RemainingTexelFetchBudget -= texels;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void OnPresent () {
			if (Config.AsyncScaling.CanFetchAndLoadSameFrame || !PushedUpdateThisFrame) {
				ScaledTexture.ProcessPendingActions(100);
			}
			RemainingTexelFetchBudget = Config.AsyncScaling.TexelFetchFrameBudget;
			FetchedThisFrame = false;
			PushedUpdateThisFrame = false;
		}

		internal static bool Present () {
			OnPresent();
			return true;
		}

		internal static bool Present (Rectangle? sourceRectangle, Rectangle? destinationRectangle, IntPtr overrideWindowHandle) {
			OnPresent();
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool OnBegin (
			SpriteBatch __instance,
			SpriteSortMode sortMode = SpriteSortMode.Deferred,
			BlendState blendState = null,
			SamplerState samplerState = null,
			DepthStencilState depthStencilState = null,
			RasterizerState rasterizerState = null,
			Effect effect = null,
			Matrix? transformMatrixRef = null
		) {
			blendState ??= BlendState.AlphaBlend;
			samplerState ??= SamplerState.LinearClamp;
			depthStencilState ??= DepthStencilState.Default;
			rasterizerState ??= RasterizerState.CullCounterClockwise;
			var transformMatrix = (transformMatrixRef != null) ? transformMatrixRef.GetValueOrDefault(Matrix.Identity) : Matrix.Identity;

			SetCurrentAddressMode(samplerState);
			CurrentBlendSourceMode = blendState.AlphaSourceBlend;

			var OriginalState = __instance.GraphicsDevice.SamplerStates[0];
			var newState = new SamplerState() {
				AddressU = OriginalState.AddressU,
				AddressV = OriginalState.AddressV,
				AddressW = OriginalState.AddressW,
				MaxAnisotropy = OriginalState.MaxAnisotropy,
				MaxMipLevel = OriginalState.MaxMipLevel,
				MipMapLevelOfDetailBias = OriginalState.MipMapLevelOfDetailBias,
				Name = "RescaledSampler",
				Tag = OriginalState.Tag,
				Filter = Config.DrawState.SetLinear ? TextureFilter.Linear : OriginalState.Filter
			};

			__instance.GraphicsDevice.SamplerStates[0] = newState;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool OnBegin2 (
			SpriteBatch __instance,
			ref SamplerState samplerState,
			SpriteSortMode sortMode = SpriteSortMode.Deferred,
			BlendState blendState = null,
			DepthStencilState depthStencilState = null,
			RasterizerState rasterizerState = null,
			Effect effect = null,
			Matrix? transformMatrixRef = null
		) {
			blendState ??= BlendState.AlphaBlend;
			samplerState ??= SamplerState.LinearClamp;
			depthStencilState ??= DepthStencilState.Default;
			rasterizerState ??= RasterizerState.CullCounterClockwise;
			var transformMatrix = (transformMatrixRef != null) ? transformMatrixRef.GetValueOrDefault(Matrix.Identity) : Matrix.Identity;

			SetCurrentAddressMode(samplerState);
			CurrentBlendSourceMode = blendState.AlphaSourceBlend;

			ResetSamplerState(ref samplerState);
			/*
			var OriginalState = samplerState;
			var newState = new SamplerState() {
				AddressU = OriginalState.AddressU,
				AddressV = OriginalState.AddressV,
				AddressW = OriginalState.AddressW,
				MaxAnisotropy = OriginalState.MaxAnisotropy,
				MaxMipLevel = OriginalState.MaxMipLevel,
				MipMapLevelOfDetailBias = OriginalState.MipMapLevelOfDetailBias,
				Name = "RescaledSampler",
				Tag = OriginalState.Tag,
				Filter = Config.DrawState.SetLinear ? TextureFilter.Linear : OriginalState.Filter
			};

			samplerState = newState;
			*/
			return true;
		}

		internal static void ResetSamplerState(ref SamplerState samplerState) {
			var OriginalState = samplerState;
			var newState = new SamplerState() {
				AddressU = TextureAddressMode.Wrap,
				AddressV = TextureAddressMode.Wrap,
				AddressW = OriginalState.AddressW,
				MaxAnisotropy = OriginalState.MaxAnisotropy,
				MaxMipLevel = OriginalState.MaxMipLevel,
				MipMapLevelOfDetailBias = OriginalState.MipMapLevelOfDetailBias,
				Name = "RescaledSampler",
				Tag = OriginalState.Tag,
				Filter = Config.DrawState.SetLinear ? TextureFilter.Linear : OriginalState.Filter
			};

			samplerState = newState;
		}

		internal static bool Begin (SpriteBatch __instance) {
			return OnBegin(__instance);
		}
		internal static bool Begin (SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState) {
			return OnBegin(__instance, sortMode, blendState);
		}
		internal static bool Begin (SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState, ref SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState) {
			return OnBegin2(__instance, ref samplerState, sortMode, blendState, depthStencilState, rasterizerState);
		}
		internal static bool Begin (SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState, ref SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect) {
			return OnBegin2(__instance, ref samplerState, sortMode, blendState, depthStencilState, rasterizerState, effect);
		}
		internal static bool Begin (SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState, ref SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix transformMatrix) {
			return OnBegin2(__instance, ref samplerState, sortMode, blendState, depthStencilState, rasterizerState, effect, transformMatrix);
		}

		private static FieldInfo SamplerStateField = typeof(SpriteBatch).GetField("samplerState", BindingFlags.Instance | BindingFlags.NonPublic);

		private sealed class AddressModeHandler : IDisposable {
			private const bool Enabled = Config.Resample.EnableWrappedAddressing;
			private readonly SamplerState OriginalState = SamplerState.PointClamp;
			private readonly SpriteBatch Batch = null;

			internal AddressModeHandler (in SpriteBatch spriteBatch, in ScaledTexture texture) {
				if (!Enabled) {
					return;
				}

				Batch = spriteBatch;

				OriginalState = spriteBatch.GraphicsDevice.SamplerStates[0];
				var newState = new SamplerState() {
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

				var ti = Batch.GetType().GetTypeInfo();
				// void SetPlatformRenderState()
				// void SetRenderState()

				spriteBatch.GraphicsDevice.SamplerStates[0] = newState;
				SamplerStateField.SetValue(spriteBatch, newState);
				typeof(SpriteBatch).GetMethod("SetRenderState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Batch, null);
				typeof(SpriteBatch).GetMethod("SetPlatformRenderState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Batch, null);
			}

			public void Dispose () {
				if (!Enabled) {
					return;
				}

				Batch.GraphicsDevice.SamplerStates[0] = OriginalState;
				SamplerStateField.SetValue(Batch, OriginalState);
				typeof(SpriteBatch).GetMethod("SetRenderState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Batch, null);
				typeof(SpriteBatch).GetMethod("SetPlatformRenderState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Batch, null);
			}
		}

		internal static readonly ThreadLocal<bool> ReentranceLock = new ThreadLocal<bool>(false);
		private sealed class ReentranceLockWrapper : IDisposable {
			private readonly ThreadLocal<bool> ReentranceLock;

			internal ReentranceLockWrapper (in ThreadLocal<bool> reentranceLock) {
				this.ReentranceLock = reentranceLock;
				ReentranceLock.Value = true;
			}

			public void Dispose () {
				ReentranceLock.Value = false;
			}
		}

		private static ScaledTexture DrawHandler (
			in SpriteBatch @this,
			Texture2D texture,
			ref Rectangle sourceRectangle,
			bool allowPadding
		) {
			try {
				if (Config.ClampInvalidBounds) {
					sourceRectangle = sourceRectangle.ClampTo(new Rectangle(0, 0, texture.Width, texture.Height));
				}

				// Let's just skip potentially invalid draws since I have no idea what to do with them.
				if (sourceRectangle.Height <= 0 || sourceRectangle.Width <= 0) {
					return null;
				}

				ScaledTexture scaledTexture = null;
				// Load textures on the fly.
				if (!(texture is RenderTarget2D) && texture.Width >= 1 && texture.Height >= 1) {
					var indexRectangle = new Rectangle(
						sourceRectangle.X,
						sourceRectangle.Y,
						sourceRectangle.Width,
						sourceRectangle.Height
					);
					scaledTexture = ScaledTexture.Get(texture, sourceRectangle, indexRectangle, allowPadding);
				}

				if (scaledTexture != null && scaledTexture.IsReady) {
					var t = scaledTexture.Texture;

					if (t == null)
						return null;

					if (Config.Resample.DeSprite && scaledTexture.IsSprite) {
						sourceRectangle = new Rectangle(
							0,
							0,
							t.Width,
							t.Height
						);
					}
					else {
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
			catch (Exception ex) {
				Debug.ErrorLn($"Exception In DrawHandler: {ex.Message}");
				Debug.ErrorLn(ex.GetStackTrace());
			}

			return null;
		}

		// new AddressModeHandler(__instance, scaledTexture)

		internal static bool DrawPatched (
			in SpriteBatch @this,
			ref Texture2D texture,
			ref Rectangle destination,
			ref Rectangle? source,
			in Color color,
			in float rotation,
			ref Vector2 origin,
			in SpriteEffects effects,
			in float layerDepth
		) {
			var sourceRectangle = source.GetValueOrDefault(new Rectangle(0, 0, texture.Width, texture.Height));
			var originalSourceRectangle = sourceRectangle;

			bool allowPadding = true;
			//origin.X == 0 && origin.Y == 0;

			var scaledTexture = DrawHandler(@this, texture, ref sourceRectangle, allowPadding);
			if (scaledTexture == null) {
				return true;
			}

			var t = scaledTexture.Texture;

			if (!scaledTexture.Padding.IsZero) {
				// Convert the draw into the other draw style. This has to be done because the padding potentially has
				// subpixel accuracy when scaled to the destination rectangle.

				var originalSize = new Vector2(originalSourceRectangle.Width, originalSourceRectangle.Height);
				var destinationSize = new Vector2(destination.Width, destination.Height);
				var newScale = destinationSize / originalSize;
				var newPosition = new Vector2(destination.X, destination.Y);

				@this.Draw(
					texture: texture,
					position: newPosition,
					sourceRectangle: source,
					color: color,
					rotation: rotation,
					origin: origin,
					scale: newScale,
					effects: effects,
					layerDepth: layerDepth
				);
				return false;
			}

			var scaledOrigin = origin / scaledTexture.Scale;

			if (Config.DrawState.SetLinear) {
				try {
					using (new ReentranceLockWrapper(ReentranceLock)) {
						using (new AddressModeHandler(@this, scaledTexture)) {
							@this.Draw(
								texture: t,
								destinationRectangle: destination,
								sourceRectangle: sourceRectangle,
								color: color,
								rotation: rotation,
								origin: scaledOrigin,
								effects: effects,
								layerDepth: layerDepth
							);
						}
					}
				}
				catch (Exception ex) {
					Debug.ErrorLn($"Exception Caught While Drawing: {ex.Message}");
				}
			}
			else {
				source = sourceRectangle;
				origin = scaledOrigin;
				texture = t;

				return true;
			}

			return false;
		}
		internal static bool DrawPatched (
			in SpriteBatch @this,
			ref Texture2D texture,
			ref Vector2 position,
			ref Rectangle? source,
			in Color color,
			in float rotation,
			ref Vector2 origin,
			ref Vector2 scale,
			in SpriteEffects effects,
			in float layerDepth
		) {
			// TODO : We need to intgrate the origin into the bounds so we can properly hash-sprite these calls.

			var sourceRectangle = source.GetValueOrDefault(new Rectangle(0, 0, texture.Width, texture.Height));
			bool allowPadding = true;
			//origin.X == 0 && origin.Y == 0;

			var scaledTexture = DrawHandler(@this, texture, ref sourceRectangle, allowPadding);
			if (scaledTexture == null) {
				return true;
			}

			var t = scaledTexture.Texture;

			var adjustedScale = scale / scaledTexture.Scale;
			var adjustedPosition = position;
			var adjustedOrigin = origin;

			if (!scaledTexture.Padding.IsZero) {
				var textureSize = new Vector2(t.Width, t.Height);
				var innerSize = new Vector2(scaledTexture.UnpaddedSize.Width, scaledTexture.UnpaddedSize.Height);

				// This is the scale factor to bring the inner size to the draw size.
				var innerRatio = textureSize / innerSize;

				// Scale the... scale by the scale factor.
				adjustedScale *= innerRatio;

				// This is the new size the sprite will draw on the screen.
				var drawSize = textureSize * adjustedScale;
				// This is the new size the inner sprite will draw on the screen.
				var scaledInnerSize = innerSize * adjustedScale;

				// This is the size of an edge of padding.
				var paddingSize = (drawSize - scaledInnerSize) * 0.5f;

				adjustedPosition -= paddingSize;

				adjustedOrigin *= scaledTexture.Scale;
				adjustedOrigin /= innerRatio;
			}
			else {
				adjustedOrigin *= scaledTexture.Scale;
			}

			if (Config.DrawState.SetLinear) {
				try {
					using (new ReentranceLockWrapper(ReentranceLock)) {
						using (new AddressModeHandler(@this, scaledTexture)) {
							@this.Draw(
								texture: t,
								position: adjustedPosition,
								sourceRectangle: sourceRectangle,
								color: color,
								rotation: rotation,
								origin: adjustedOrigin,
								scale: adjustedScale,
								effects: effects,
								layerDepth: layerDepth
							);
						}
					}
				}
				catch (Exception ex) {
					Debug.ErrorLn($"Exception Caught While Drawing: {ex.Message}");
				}
			}
			else {
				texture = t;
				source = sourceRectangle;
				origin = adjustedOrigin;
				scale = adjustedScale;
				position = adjustedPosition;
				return true;
			}

			return false;
		}

		private partial class Harmony {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static bool Draw (
				in SpriteBatch __instance,
				ref Texture2D texture,
				ref Rectangle destinationRectangle,
				ref Rectangle? sourceRectangle,
				in Color color,
				in float rotation,
				ref Vector2 origin,
				in SpriteEffects effects,
				in float layerDepth
			) {
				if (!Config.Enabled || ReentranceLock.Value)
					return true;

				return DrawPatched(
					@this: __instance,
					texture: ref texture,
					destination: ref destinationRectangle,
					source: ref sourceRectangle,
					color: color,
					rotation: rotation,
					origin: ref origin,
					effects: effects,
					layerDepth: layerDepth
				);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static bool Redraw (
				in SpriteBatch @this,
				in Texture2D texture,
				in Rectangle destinationRectangle,
				in Color color,
				in Rectangle? sourceRectangle = null,
				in float rotation = 0f,
				in Vector2? origin = null,
				in SpriteEffects effects = SpriteEffects.None,
				in float layerDepth = 0f
			) {
				if (!Config.Enabled || ReentranceLock.Value)
					return true;

				@this.Draw(
					texture: texture,
					destinationRectangle: destinationRectangle,
					sourceRectangle: sourceRectangle,
					color: color,
					rotation: rotation,
					origin: origin ?? Vector2.Zero,
					effects: effects,
					layerDepth: layerDepth
				);

				return false;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static bool Draw (SpriteBatch __instance, Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color) {
				return Redraw(
					@this: __instance,
					texture: texture,
					destinationRectangle: destinationRectangle,
					sourceRectangle: sourceRectangle,
					color: color
				);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static bool Draw (SpriteBatch __instance, Texture2D texture, Rectangle destinationRectangle, Color color) {
				return Redraw(
					@this: __instance,
					texture: texture,
					destinationRectangle: destinationRectangle,
					color: color
				);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static bool Redraw (
				in SpriteBatch @this,
				in Texture2D texture,
				in Vector2 position,
				in Color color,
				in Rectangle? sourceRectangle = null,
				in float rotation = 0f,
				in Vector2? origin = null,
				in Vector2? scale = null,
				in SpriteEffects effects = SpriteEffects.None,
				in float layerDepth = 0f
			) {
				if (!Config.Enabled || ReentranceLock.Value)
					return true;

				@this.Draw(
					texture: texture,
					position: position,
					sourceRectangle: sourceRectangle,
					color: color,
					rotation: rotation,
					origin: origin ?? Vector2.Zero,
					scale: scale ?? Vector2.One,
					effects: effects,
					layerDepth: layerDepth
				);

				return false;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static bool Draw (SpriteBatch __instance, ref Texture2D texture, ref Vector2 position, ref Rectangle? sourceRectangle, Color color, float rotation, ref Vector2 origin, ref Vector2 scale, SpriteEffects effects, float layerDepth) {
				if (!Config.Enabled || ReentranceLock.Value)
					return true;

				return DrawPatched(
					@this: __instance,
					texture: ref texture,
					position: ref position,
					source: ref sourceRectangle,
					color: color,
					rotation: rotation,
					origin: ref origin,
					scale: ref scale,
					effects: effects,
					layerDepth: layerDepth
				);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static bool Draw (SpriteBatch __instance, Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth) {
				return Redraw(
					@this: __instance,
					texture: texture,
					position: position,
					sourceRectangle: sourceRectangle,
					color: color,
					rotation: rotation,
					origin: origin,
					scale: new Vector2(scale),
					effects: effects,
					layerDepth: layerDepth
				);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static bool Draw (SpriteBatch __instance, Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color) {
				return Redraw(
					@this: __instance,
					texture: texture,
					position: position,
					sourceRectangle: sourceRectangle,
					color: color
				);
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static bool Draw (SpriteBatch __instance, Texture2D texture, Vector2 position, Color color) {
				return Redraw(
					@this: __instance,
					texture: texture,
					position: position,
					color: color
				);
			}
		}
	}
}
