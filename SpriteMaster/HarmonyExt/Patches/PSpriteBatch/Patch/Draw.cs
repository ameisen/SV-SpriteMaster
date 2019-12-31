using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SpriteMaster.HarmonyExt.Patches.PSpriteBatch.Patch {
	[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Harmony")]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Harmony")]
	internal static class Draw {
		/*
		 * All patches that have fewer arguments than the two primary .Draw methods are forwarded to the ones with more arguments, since we will override those arguments.
		 * This also means that they must be actually FIRST so we can effectively prevent other mods/overrides from altering their arguments, since when they call .Draw again,
		 * those mods would then alter the arguments _again_, causing issues.
		 * 
		 * Previously, the logic would be like this:
		 * Draw -> OTHERMOD.Draw -> SpriteMaster.Draw -> DrawMoreArguments -> OTHERMOD.DrawMoreArguments -> SpriteMaster.DrawMoreArguments
		 * 
		 * It is now:
		 * 
		 * Draw -> SpriteMaster.Draw -> DrawMoreArguments -> OTHERMOD.DrawMoreArguments -> SpriteMaster.DrawMoreArguments
		 * 
		 */

		/*
		[HarmonyPatch(typeof(SpriteBatch), "BackToFrontComparer", "Compare", isChild: true, HarmonyPatch.Fixation.Prefix, HarmonyExt.PriorityLevel.First)]
		internal static bool BFComparer (object __instance, ref int __result, int x, int y) {
			var batch = (SpriteBatch)__instance.GetField("parent");
			var queue = (object[])batch.GetField("spriteQueue");
			var DepthGetter = queue.GetType().GetElementType().GetField("Depth", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

			var queueGetter = queue.GetType().GetMethod("GetValue", new Type[] { typeof(int) });

			var depth = (float)DepthGetter.GetValue(queueGetter.Invoke(queue, new object[] { x }));
			var depth2 = (float)DepthGetter.GetValue(queueGetter.Invoke(queue, new object[] { y }));
			if (depth > depth2) {
				__result = -1;
				return false;
			}
			if (depth < depth2) {
				__result = 1;
				return false;
			}

			__result = y.CompareTo(x);
			return false;
		}

		[HarmonyPatch(typeof(SpriteBatch), "FrontToBackComparer", "Compare", isChild: true, HarmonyPatch.Fixation.Prefix, HarmonyExt.PriorityLevel.First)]
		internal static bool FBComparer (object __instance, ref int __result, int x, int y) {
			var batch = (SpriteBatch)__instance.GetField("parent");
			var queue = batch.GetField("spriteQueue");
			var DepthGetter = queue.GetType().GetElementType().GetField("Depth", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

			var queueGetter = queue.GetType().GetMethod("GetValue", new Type[] { typeof(int) });

			var depth = (float)DepthGetter.GetValue(queueGetter.Invoke(queue, new object[] { x }));
			var depth2 = (float)DepthGetter.GetValue(queueGetter.Invoke(queue, new object[] { y }));
			if (depth > depth2) {
				__result = 1;
				return false;
			}
			if (depth < depth2) {
				__result = -1;
				return false;
			}
			__result = x.CompareTo(y);
			return false;
		}
		*/

		/*
		[HarmonyPatch("InternalDraw", fixation: HarmonyPatch.Fixation.Prefix, priority: HarmonyExt.PriorityLevel.Last)]
		internal static bool OnInternalDraw (
			SpriteBatch __instance,
			ref Texture2D texture,
			ref Vector4 destination,
			ref bool scaleDestination,
			ref Rectangle? sourceRectangle,
			Color color,
			float rotation,
			ref Vector2 origin,
			SpriteEffects effects,
			ref float depth
		) {
			//if (!Config.Enabled)
			//	return true;

			var originalSize = sourceRectangle.GetValueOrDefault(new Rectangle(0, 0, texture.Width, texture.Height));

			// Temporary hack for water.
			if (texture.Name == "LooseSprites\\Cursors" && originalSize.Right <= 640 && originalSize.Top >= 2000) {
				//Debug.InfoLn($"Water?: {originalSize.X} {originalSize.Y}");
				// depth = 0.56
				//depth -= 0.5f;
				return true;
			}

			if (texture is RenderTarget2D || originalSize.Width <= 1 || originalSize.Height <= 1 || texture.Width <= 1 || texture.Height <= 1 || Math.Max(originalSize.Width, originalSize.Height) <= Config.Resample.MinimumTextureDimensions) {
				return true;
			}

			if (!scaleDestination) {
				//originalSize = originalSize.ClampTo(new Rectangle(0, 0, texture.Width, texture.Height));

				var destinationSize = new Vector2(destination.Z, destination.W);
				var newScale = destinationSize / new Vector2(originalSize.Width, originalSize.Height);
				destination.Z = newScale.X;
				destination.W = newScale.Y;
				scaleDestination = true;
			}

			var scale = new Vector2(destination.Z, destination.W);
			var position = new Vector2(destination.X, destination.Y);

			if (Config.Enabled) {
				__instance.OnDraw(
					texture: ref texture,
					position: ref position,
					source: ref sourceRectangle,
					color: color,
					rotation: rotation,
					origin: ref origin,
					scale: ref scale,
					effects: effects,
					layerDepth: ref depth
				);
			}

			destination = new Vector4(position, scale.X, scale.Y);
			return true;
		}
		*/

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[HarmonyPatch("Draw", priority: HarmonyExt.PriorityLevel.First)]
		internal static bool OnDrawFirst (
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

			return __instance.OnDrawFirst(
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
		[HarmonyPatch("Draw", priority: HarmonyExt.PriorityLevel.Last)]
		internal static bool OnDrawLast (
			SpriteBatch __instance,
			ref Texture2D texture,
			ref Rectangle destinationRectangle,
			ref Rectangle? sourceRectangle,
			Color color,
			float rotation,
			ref Vector2 origin,
			SpriteEffects effects,
			ref float layerDepth
		) {
			if (!Config.Enabled)
				return true;

			return __instance.OnDraw(
				texture: ref texture,
				destination: ref destinationRectangle,
				source: ref sourceRectangle,
				color: color,
				rotation: rotation,
				origin: ref origin,
				effects: effects,
				layerDepth: ref layerDepth
			);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool ForwardDraw (
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
		[HarmonyPatch("Draw", priority: HarmonyExt.PriorityLevel.First)]
		internal static bool OnDraw (SpriteBatch __instance, Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color) {
			return ForwardDraw(
				@this: __instance,
				texture: texture,
				destinationRectangle: destinationRectangle,
				sourceRectangle: sourceRectangle,
				color: color
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[HarmonyPatch("Draw", priority: HarmonyExt.PriorityLevel.First)]
		internal static bool OnDraw (SpriteBatch __instance, Texture2D texture, Rectangle destinationRectangle, Color color) {
			return ForwardDraw(
				@this: __instance,
				texture: texture,
				destinationRectangle: destinationRectangle,
				color: color
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool ForwardDraw (
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
		[HarmonyPatch("Draw", priority: HarmonyExt.PriorityLevel.Last)]
		internal static bool OnDraw (SpriteBatch __instance, ref Texture2D texture, ref Vector2 position, ref Rectangle? sourceRectangle, Color color, float rotation, ref Vector2 origin, ref Vector2 scale, SpriteEffects effects, float layerDepth) {
			if (!Config.Enabled)
				return true;

			return __instance.OnDraw(
				texture: ref texture,
				position: ref position,
				source: ref sourceRectangle,
				color: color,
				rotation: rotation,
				origin: ref origin,
				scale: ref scale,
				effects: effects,
				layerDepth: ref layerDepth
			);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[HarmonyPatch("Draw", priority: HarmonyExt.PriorityLevel.First)]
		internal static bool OnDraw (SpriteBatch __instance, Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth) {
			return ForwardDraw(
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
		[HarmonyPatch("Draw", priority: HarmonyExt.PriorityLevel.First)]
		internal static bool OnDraw (SpriteBatch __instance, Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color) {
			return ForwardDraw(
				@this: __instance,
				texture: texture,
				position: position,
				sourceRectangle: sourceRectangle,
				color: color
			);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[HarmonyPatch("Draw", priority: HarmonyExt.PriorityLevel.First)]
		internal static bool OnDraw (SpriteBatch __instance, Texture2D texture, Vector2 position, Color color) {
			return ForwardDraw(
				@this: __instance,
				texture: texture,
				position: position,
				color: color
			);
		}
	}
}
