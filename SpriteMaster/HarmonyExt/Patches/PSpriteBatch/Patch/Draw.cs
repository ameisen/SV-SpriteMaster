using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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

#if STABLE_SORT
		private static class Comparer {
			private static FieldInfo TextureField;
			private static FieldInfo SpriteField;
			private static MethodInfo TextureComparer;
			private static MethodInfo SpriteGetter;
			private static FieldInfo GetSource;
			private static FieldInfo GetOrigin;

			static Comparer () {
				TextureField = typeof(SpriteBatch).GetField("spriteTextures", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				SpriteField = typeof(SpriteBatch).GetField("spriteQueue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				TextureComparer = typeof(Texture).GetMethod("CompareTo", BindingFlags.Instance | BindingFlags.NonPublic);
				SpriteGetter = SpriteField.FieldType.GetMethod("GetValue", new Type[] { typeof(int) });
				GetSource = SpriteField.FieldType.GetElementType().GetField("Source", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				GetOrigin = SpriteField.FieldType.GetElementType().GetField("Origin", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}

			static int Compare (in Vector2 lhs, in Vector2 rhs) {
				if (lhs.X < rhs.X) {
					return -1;
				}
				else if (lhs.X > rhs.X) {
					return 1;
				}

				if (lhs.Y < rhs.Y) {
					return -1;
				}
				else if (lhs.Y > rhs.Y) {
					return 1;
				}

				return 0;
			}

			static int Compare (in Vector4 lhs, in Vector4 rhs) {
				int comparison = Compare(new Vector2(lhs.X, lhs.Y), new Vector2(rhs.X, rhs.Y));
				if (comparison != 0) {
					return comparison;
				}

				return Compare(new Vector2(lhs.Z, lhs.W), new Vector2(rhs.Z, rhs.W));
			}

			[HarmonyPatch(typeof(SpriteBatch), "BackToFrontComparer", "Compare", isChild: true, HarmonyPatch.Fixation.Postfix, HarmonyExt.PriorityLevel.Last)]
			internal static void BFComparer (object __instance, ref int __result, SpriteBatch ___parent, int x, int y) {
				if (__result != 0)
					return;

				var textures = (Texture[])TextureField.GetValue(___parent);

				var texture1 = textures[x];
				var texture2 = textures[y];

				var comparison = (int)TextureComparer.Invoke(texture1, new object[] { texture2 });
				if (comparison != 0) {
					__result = comparison;
					return;
				}

				var spriteQueue = SpriteField.GetValue(___parent);

				var sprite1 = SpriteGetter.Invoke(spriteQueue, new object[] { x });
				var sprite2 = SpriteGetter.Invoke(spriteQueue, new object[] { y });

				var source1 = (Vector4)GetSource.GetValue(sprite1);
				var source2 = (Vector4)GetSource.GetValue(sprite2);

				comparison = Compare(source1, source2);
				if (comparison != 0) {
					__result = comparison;
					return;
				}

				var origin1 = (Vector2)GetOrigin.GetValue(sprite1);
				var origin2 = (Vector2)GetOrigin.GetValue(sprite2);

				comparison = Compare(origin1, origin2);
				if (comparison != 0) {
					__result = comparison;
					return;
				}

				__result = y.CompareTo(x);
			}

			[HarmonyPatch(typeof(SpriteBatch), "FrontToBackComparer", "Compare", isChild: true, HarmonyPatch.Fixation.Postfix, HarmonyExt.PriorityLevel.Last)]
			internal static void FBComparer (object __instance, ref int __result, SpriteBatch ___parent, int x, int y) {
				if (__result != 0)
					return;

				var textures = (Texture[])TextureField.GetValue(___parent);

				var texture1 = textures[y];
				var texture2 = textures[x];

				var comparison = (int)TextureComparer.Invoke(texture1, new object[] { texture2 });
				if (comparison != 0) {
					__result = comparison;
					return;
				}

				var spriteQueue = SpriteField.GetValue(___parent);

				var sprite1 = SpriteGetter.Invoke(spriteQueue, new object[] { y });
				var sprite2 = SpriteGetter.Invoke(spriteQueue, new object[] { x });

				var source1 = (Vector4)GetSource.GetValue(sprite1);
				var source2 = (Vector4)GetSource.GetValue(sprite2);

				comparison = Compare(source1, source2);
				if (comparison != 0) {
					__result = comparison;
					return;
				}

				var origin1 = (Vector2)GetOrigin.GetValue(sprite1);
				var origin2 = (Vector2)GetOrigin.GetValue(sprite2);

				comparison = Compare(origin1, origin2);
				if (comparison != 0) {
					__result = comparison;
					return;
				}

				__result = x.CompareTo(y);
			}
		}
#endif

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
