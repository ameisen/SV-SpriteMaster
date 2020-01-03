using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static SpriteMaster.HarmonyExt.HarmonyExt;
using static SpriteMaster.ScaledTexture;

namespace SpriteMaster.HarmonyExt.Patches {
	[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Harmony")]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Harmony")]
	internal static class PTexture2D {
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

			if (!ScaledTexture.Validate(__instance)) {
				return;
			}

			var dataRef = DataRef<byte>.Null;
			if (__instance.LevelCount <= 1) {
				dataRef = (byte[])data.AsSpan().CastAs<T, byte>().ToArray().Clone();
			}

			ScaledTexture.Purge(__instance, null, dataRef);
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

			if (!ScaledTexture.Validate(__instance)) {
				return;
			}

			var dataRef = DataRef<byte>.Null;
			if (__instance.LevelCount <= 1) {
				dataRef = new DataRef<byte>((byte[])data.AsSpan().CastAs<T, byte>().ToArray().Clone(), startIndex, elementCount);
			}

			ScaledTexture.Purge(__instance, null, dataRef);
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

			if (!ScaledTexture.Validate(__instance)) {
				return;
			}

			var dataRef = DataRef<byte>.Null;
			if (__instance.LevelCount <= 1) {
				dataRef = new DataRef<byte>((byte[])data.AsSpan().CastAs<T, byte>().ToArray().Clone(), startIndex, elementCount);
			}

			ScaledTexture.Purge(__instance, rect, dataRef);
		}

		// A horrible, horrible hack to stop a rare-ish crash when zooming or when the device resets. It doesn't appear to originate in SpriteMaster, but SM most certainly
		// makes it worse. This will force the texture to regenerate on the fly if it is in a zombie state.
		[HarmonyPatch("Microsoft.Xna.Framework", "Microsoft.Xna.Framework.Helpers", "CheckDisposed", HarmonyPatch.Fixation.Prefix, PriorityLevel.Last, instance: false)]
		private static unsafe bool CheckDisposed (object obj, ref IntPtr pComPtr) {
			if (obj is GraphicsResource resource) {
				if (pComPtr == IntPtr.Zero || resource.IsDisposed) {
					if (!resource.IsDisposed) {
						resource.Dispose();
					}

					if (resource is Texture2D texture) {
						Debug.WarningLn("CheckDisposed is going to throw, attempting to restore state");

						// TODO : we should probably use the helper function it calls instead, just in case the user defined a child class.
						var ctor = texture.GetType().GetConstructor(
							BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
							null,
							new Type[] {
								typeof(GraphicsDevice),
								typeof(int),
								typeof(int),
								typeof(bool),
								typeof(SurfaceFormat)
							},
							null
						);

						ctor.Invoke(texture, new object[] { DrawState.Device, texture.Width, texture.Height, texture.LevelCount > 1, texture.Format });
						//pComPtr = (IntPtr)(void*)texture.GetField("pComPtr");
						return false;
					}
				}
			}
			return true;
		}

		/*
		[HarmonyPatch(typeof(Texture), "GetAndValidateSizes", HarmonyPatch.Fixation.Prefix, PriorityLevel.First, HarmonyPatch.Generic.Struct, instance: false)]
		private unsafe static bool GetAndValidateSizes<T> (int* pSurface, uint* pdwFormatSize, uint* pdwElementSize) where T : struct {
			if (pSurface == null || pdwFormatSize == null || pdwElementSize == null) {
				return true;
			}

			var GetSize = typeof(Texture).GetMethod("GetExpectedByteSizeFromFormat", BindingFlags.Static | BindingFlags.NonPublic);

			var format = *(int*)pSurface;
			var arguments = new object[] { format };
			*pdwFormatSize = (uint)(byte)GetSize.Invoke(null, arguments);

			return false;
		}

		[HarmonyPatch(typeof(Texture), "ValidateTotalSize", HarmonyPatch.Fixation.Prefix, PriorityLevel.First, instance: false)]
		private unsafe static bool ValidateTotalSize (int* __unnamed000, uint dwLockWidth, uint dwLockHeight, uint dwFormatSize, uint dwElementSize, uint elementCount) {
			return false;
		}
		*/

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
