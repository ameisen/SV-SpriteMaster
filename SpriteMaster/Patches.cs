using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static SpriteMaster.ScaledTexture;

namespace SpriteMaster {

	using DrawState = Config.DrawState;

	static class Extension {
		internal static int ToCoordinate (this float coordinate) {
			return coordinate.NearestInt();
		}

		internal static int ToCoordinate (this double coordinate) {
			return coordinate.NearestInt();
		}
	}

	internal sealed class Patches {
		private partial class Harmony {
			private static Type[] GetMethodParameters (MethodInfo method) {
				var filteredParameters = method.GetParameters().Where(t => !t.Name.StartsWith("__"));
				return filteredParameters.Select(p => p.ParameterType).ToArray();
			}

			internal static MethodInfo GetPatchMethod<T> (string name, MethodInfo method) {
				var type = typeof(T);
				var methodParameters = GetMethodParameters(method);
				var typeMethod = type.GetMethod(name, methodParameters);
				if (typeMethod == null) {
					var typeMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(t => t.Name == name);
					foreach (var testMethod in typeMethods) {
						// Compare the parameters. Ignore references.
						var testParameters = testMethod.GetParameters();
						if (testParameters.Length != methodParameters.Length) {
							continue;
						}

						bool found = true;
						foreach (var i in 0.Until(testParameters.Length)) {
							var testParameter = testParameters[i].ParameterType.RemoveRef();
							var testParameterRef = testParameter.AddRef();
							var testBaseParameter = testParameter.IsArray ? testParameter.GetElementType() : testParameter;
							var methodParameter = methodParameters[i].RemoveRef();
							var methodParameterRef = methodParameter.AddRef();
							var baseParameter = methodParameter.IsArray ? methodParameter.GetElementType() : methodParameter;
							if (
								!testParameterRef.Equals(methodParameterRef) &&
								!(testBaseParameter.IsGenericParameter && baseParameter.IsGenericParameter) &&
								!methodParameter.Equals(typeof(object)) && !(testParameter.IsArray && methodParameter.IsArray && baseParameter.Equals(typeof(object)))) {
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
						return null;
					}
				}
				return typeMethod;
			}

			internal static void Patch<T> (HarmonyInstance instance, string name, MethodInfo pre = null, MethodInfo post = null, int priority = Priority.Last) {
				var referenceMethod = pre ?? post;
				var typeMethod = GetPatchMethod<T>(name, referenceMethod);
				instance.Patch(
					typeMethod,
					(pre == null) ? null : new HarmonyMethod(pre) { prioritiy = priority },
					(post == null) ? null : new HarmonyMethod(post) { prioritiy = priority },
					null
				);
			}

			internal static void Patch<T> (HarmonyInstance instance, string name, IEnumerable<MethodInfo> pre = default, IEnumerable<MethodInfo> post = default, int priority = Priority.Last) {
				if (pre != null)
					foreach (var method in pre) {
						Patch<T>(instance, name, pre: method, post: null, priority);
					}
				if (post != null)
					foreach (var method in post) {
						Patch<T>(instance, name, pre: null, post: method, priority);
					}
			}

			internal static void Patch<T, U> (HarmonyInstance instance, string name, MethodInfo pre = null, MethodInfo post = null, int priority = Priority.Last) where U : struct {
				var referenceMethod = pre ?? post;
				var typeMethod = GetPatchMethod<T>(name, referenceMethod).MakeGenericMethod(typeof(U));
				instance.Patch(
					typeMethod,
					(pre == null) ? null : new HarmonyMethod(pre.MakeGenericMethod(typeof(U))) { prioritiy = priority },
					(post == null) ? null : new HarmonyMethod(post.MakeGenericMethod(typeof(U))) { prioritiy = priority },
					null
				);
			}

			internal static void Patch<T, U> (HarmonyInstance instance, string name, IEnumerable<MethodInfo> pre = default, IEnumerable<MethodInfo> post = default, int priority = Priority.Last) where U : struct {
				if (pre != null)
					foreach (var method in pre) {
						Patch<T, U>(instance, name, pre: method, post: null, priority);
					}
				if (post != null)
					foreach (var method in post) {
						Patch<T, U>(instance, name, pre: null, post: method, priority);
					}
			}
		}

		internal static void InitializePatch (HarmonyInstance instance) {
			IEnumerable<MethodInfo> GetMethods<T> (string name) {
				return typeof(T).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(m => m.Name == name);
			}

			Harmony.Patch<SpriteBatch>(instance, "Draw", pre: GetMethods<Patches.Harmony>("Draw"));

			Harmony.Patch<SpriteBatch>(instance, "Begin", pre: GetMethods<Patches>("Begin"));

			Harmony.Patch<GraphicsDeviceManager>(instance, "ApplyChanges", pre: GetMethods<Patches>("ApplyChanges"), post: GetMethods<Patches>("ApplyChangesPost"));

			Harmony.Patch<GraphicsDevice>(instance, "Present", pre: GetMethods<Patches>("Present"));

			Harmony.Patch<SpriteBatch>(instance, "PlatformRenderBatch", pre: GetMethods<Patches>("PlatformRenderBatch"), post: GetMethods<Patches>("PlatformRenderBatchPost"));

			// https://github.com/pardeike/Harmony/issues/121
			foreach (var method in GetMethods<Patches>("SetData")) {
				Harmony.Patch<Texture2D, byte>(instance, "SetData", post: method);
				Harmony.Patch<Texture2D, sbyte>(instance, "SetData", post: method);
				Harmony.Patch<Texture2D, int>(instance, "SetData", post: method);
				Harmony.Patch<Texture2D, uint>(instance, "SetData", post: method);
				Harmony.Patch<Texture2D, Color>(instance, "SetData", post: method);
				Harmony.Patch<Texture2D, System.Drawing.Color>(instance, "SetData", post: method);
			}

			//foreach (MethodInfo method in typeof(DrawPatched).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(m => m.Name == "SetData"))
			//	Harmony.Patch(instance, typeof(Texture2D), "SetData", method);
		}

		internal static bool PlatformRenderBatch (
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

		internal static void PlatformRenderBatchPost (
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


		internal static bool ApplyChanges (GraphicsDeviceManager __instance) {
			var @this = __instance;

			@this.PreferMultiSampling = DrawState.EnableMSAA;
			@this.SynchronizeWithVerticalRetrace = true;
			@this.PreferredBackBufferFormat = Config.DrawState.BackbufferFormat;
			if (DrawState.DisableDepthBuffer)
				@this.PreferredDepthStencilFormat = DepthFormat.None;

			return true;
		}

		internal static void ApplyChangesPost (GraphicsDeviceManager __instance) {
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
					if ((int)maxTextureSizeProperty.GetValue(capabilities) < Config.PreferredDimension) {
						maxTextureSizeProperty.SetValue(capabilities, Config.PreferredDimension);
						getPrivateField(capabilities, "MaxTextureAspectRatio").SetValue(capabilities, Config.PreferredDimension / 2);
						Config.ClampDimension = Config.PreferredDimension;
					}
				}
			}
			catch (Exception ex) {
				ex.PrintWarning();
			}
		}

		private static readonly SamplerState DefaultSamplerState = SamplerState.LinearClamp;
		private static bool FetchedThisFrame = false;
		private static int RemainingTexelFetchBudget = Config.AsyncScaling.TexelFetchFrameBudget;
		private static bool PushedUpdateThisFrame = false;
		public static TextureAddressMode CurrentAddressModeU = DefaultSamplerState.AddressU;
		public static TextureAddressMode CurrentAddressModeV = DefaultSamplerState.AddressV;
		public static Blend CurrentBlendSourceMode = BlendState.AlphaBlend.AlphaSourceBlend;

		internal static void SetData<T> (Texture2D __instance, T[] data) where T : struct {
			ScaledTexture.TextureMap.Purge(__instance);
		}
		internal static void SetData<T> (Texture2D __instance, T[] data, int startIndex, int elementCount) where T : struct {
			ScaledTexture.TextureMap.Purge(__instance);
		}
		internal static void SetData<T> (Texture2D __instance, int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct {
			ScaledTexture.TextureMap.Purge(__instance, rect ?? null);
		}

		private static void SetCurrentAddressMode (SamplerState samplerState) {
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
				ScaledTexture.ProcessPendingActions(1);
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

		internal static bool Begin (SpriteBatch __instance) {
			__instance.Begin(
				SpriteSortMode.Deferred,
				BlendState.AlphaBlend,
				SamplerState.LinearClamp,
				DepthStencilState.Default,
				RasterizerState.CullCounterClockwise,
				null,
				Matrix.Identity
			);
			return false;
		}
		internal static bool Begin (SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState) {
			__instance.Begin(
				sortMode,
				blendState,
				SamplerState.LinearClamp,
				DepthStencilState.Default,
				RasterizerState.CullCounterClockwise,
				null,
				Matrix.Identity
			);
			return false;
		}
		internal static bool Begin (SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState) {
			__instance.Begin(
				sortMode,
				blendState,
				samplerState,
				depthStencilState,
				rasterizerState,
				null,
				Matrix.Identity
			);
			return false;
		}
		internal static bool Begin (SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect) {
			__instance.Begin(
				sortMode,
				blendState,
				samplerState,
				depthStencilState,
				rasterizerState,
				effect,
				Matrix.Identity
			);
			return false;
		}
		internal static bool Begin (SpriteBatch __instance, SpriteSortMode sortMode, BlendState blendState, ref SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix transformMatrix) {
			SetCurrentAddressMode(samplerState);
			CurrentBlendSourceMode = blendState.AlphaSourceBlend;

			return true;
		}

		private static ScaledTexture DrawHandler (
			SpriteBatch @this,
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
				ex.PrintError();
			}

			return null;
		}

		// new AddressModeHandler(__instance, scaledTexture)

		internal static bool DrawPatched (
			SpriteBatch @this,
			ref Texture2D texture,
			ref Rectangle destination,
			ref Rectangle? source,
			Color color,
			float rotation,
			ref Vector2 origin,
			SpriteEffects effects,
			float layerDepth
		) {
			var sourceRectangle = source.GetValueOrDefault(new Rectangle(0, 0, texture.Width, texture.Height));
			var originalSourceRectangle = sourceRectangle;

			bool allowPadding = true;

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

			source = sourceRectangle;
			origin = scaledOrigin;
			texture = t;

			return true;
		}

		internal static bool DrawPatched (
			SpriteBatch @this,
			ref Texture2D texture,
			ref Vector2 position,
			ref Rectangle? source,
			Color color,
			float rotation,
			ref Vector2 origin,
			ref Vector2 scale,
			SpriteEffects effects,
			float layerDepth
		) {
			// TODO : We need to intgrate the origin into the bounds so we can properly hash-sprite these calls.

			var sourceRectangle = source.GetValueOrDefault(new Rectangle(0, 0, texture.Width, texture.Height));
			bool allowPadding = true;

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

			texture = t;
			source = sourceRectangle;
			origin = adjustedOrigin;
			scale = adjustedScale;
			position = adjustedPosition;
			return true;
		}

		private partial class Harmony {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static bool Draw (
				SpriteBatch __instance,
				ref Texture2D texture,
				ref Rectangle destinationRectangle,
				ref Rectangle? sourceRectangle,
				Color color,
				float rotation,
				ref Vector2 origin,
				SpriteEffects effects,
				float layerDepth
			) {
				if (!Config.Enabled)
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
				SpriteBatch @this,
				Texture2D texture,
				Rectangle destinationRectangle,
				Color color,
				Rectangle? sourceRectangle = null,
				float rotation = 0f,
				Vector2? origin = null,
				SpriteEffects effects = SpriteEffects.None,
				float layerDepth = 0f
			) {
				if (!Config.Enabled)
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
				SpriteBatch @this,
				Texture2D texture,
				Vector2 position,
				Color color,
				Rectangle? sourceRectangle = null,
				float rotation = 0f,
				Vector2? origin = null,
				Vector2? scale = null,
				SpriteEffects effects = SpriteEffects.None,
				float layerDepth = 0f
			) {
				if (!Config.Enabled)
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
				if (!Config.Enabled)
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
