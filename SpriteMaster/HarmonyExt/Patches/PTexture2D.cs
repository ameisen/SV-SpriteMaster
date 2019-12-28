using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using static SpriteMaster.HarmonyExt.HarmonyExt;
using static SpriteMaster.ScaledTexture;

namespace SpriteMaster.HarmonyExt.Patches {
	[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Harmony")]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Harmony")]
	static class PTexture2D {
		private static readonly MethodInfo CopyData;
		private static readonly Dictionary<Type, MethodInfo> CopyDataGeneric = new Dictionary<Type, MethodInfo>();
		static PTexture2D () {
			CopyData = typeof(Texture2D).GetMethod("CopyData", BindingFlags.Instance | BindingFlags.NonPublic);
			foreach (var type in HarmonyExt.StructTypes) {
				CopyDataGeneric.Add(type, CopyData.MakeGenericMethod(type));
			}
		}

		/*
		[HarmonyPatch("SetData", HarmonyPatch.Fixation.Prefix, PriorityLevel.First, HarmonyPatch.Generic.Struct)]
		private static bool OnSetDataPre<T> (Texture2D __instance, T[] data) where T : struct {
			if (__instance is ManagedTexture2D) {
				CopyDataGeneric[typeof(T)].Invoke(__instance, new object[] { 0, null, data, 0, data.Length, 0u, true });
				return false;
			}

			return true;
		}
		*/

		[HarmonyPatch("SetData", HarmonyPatch.Fixation.Postfix, PriorityLevel.Last, HarmonyPatch.Generic.Struct)]
		private static void OnSetDataPost<T> (Texture2D __instance, T[] data) where T : struct {
			if (__instance is ManagedTexture2D) {
				return;
			}

			ScaledTexture.Purge(__instance, null, new DataRef<T>(data));
		}

		/*
		[HarmonyPatch("SetData", HarmonyPatch.Fixation.Prefix, PriorityLevel.First, HarmonyPatch.Generic.Struct)]
		private static bool OnSetDataPre<T> (Texture2D __instance, T[] data, int startIndex, int elementCount) where T : struct {
			if (__instance is ManagedTexture2D) {
				CopyDataGeneric[typeof(T)].Invoke(__instance, new object[] { 0, null, data, startIndex, elementCount, 0u, true });
				return false;
			}

			return true;
		}
		*/

		[HarmonyPatch("SetData", HarmonyPatch.Fixation.Postfix, PriorityLevel.Last, HarmonyPatch.Generic.Struct)]
		private static void OnSetDataPost<T> (Texture2D __instance, T[] data, int startIndex, int elementCount) where T : struct {
			if (__instance is ManagedTexture2D) {
				return;
			}

			ScaledTexture.Purge(__instance, null, new DataRef<T>(data, startIndex, elementCount));
		}

		/*
		[HarmonyPatch("SetData", HarmonyPatch.Fixation.Prefix, PriorityLevel.First, HarmonyPatch.Generic.Struct)]
		private static bool OnSetDataPre<T> (Texture2D __instance, MethodBase __originalMethod, int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct {
			if (__instance is ManagedTexture2D) {
				__originalMethod.Invoke(__instance, new object[] { data, level, rect, data, startIndex, elementCount });
				return false;
			}

			return true;
		}
		*/

		[HarmonyPatch("SetData", HarmonyPatch.Fixation.Postfix, PriorityLevel.Last, HarmonyPatch.Generic.Struct)]
		private static void OnSetDataPost<T> (Texture2D __instance, int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : struct {
			if (__instance is ManagedTexture2D) {
				return;
			}

			ScaledTexture.Purge(__instance, rect, new DataRef<T>(data, startIndex, elementCount));
		}

		/*
		[HarmonyPatch(typeof(Texture2D), "FromStream", HarmonyPatch.Fixation.Postfix, PriorityLevel.Last)]
		private static void FromStream(GraphicsDevice graphicsDevice, Stream stream, int width, int height, [MarshalAs(UnmanagedType.U1)] bool zoom) {
			zoom = zoom;
		}

		[HarmonyPatch(typeof(Texture2D), "FromStream", HarmonyPatch.Fixation.Postfix, PriorityLevel.Last)]
		private static void FromStream (GraphicsDevice graphicsDevice, Stream stream) {
			stream = stream;
		}
		*/

		//public static Texture2D FromStream (GraphicsDevice graphicsDevice, Stream stream, int width, int height, bool zoom);
		//public static Texture2D FromStream (GraphicsDevice graphicsDevice, Stream stream);
	}
}
