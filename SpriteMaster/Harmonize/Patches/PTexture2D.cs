#define ASYNC_SETDATA

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Types;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using static SpriteMaster.Harmonize.Harmonize;

namespace SpriteMaster.Harmonize.Patches {
	[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Harmony")]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Harmony")]
	internal static class PTexture2D {
		// Always returns a duplicate of the array, since we do not own the source array.
		// It performs a shallow copy, which is fine.
		private static byte[] GetByteArray<T>(T[] data) where T : unmanaged {
			if (data == null) {
				return null;
			}

			if (data is byte[] _data) {
				return (byte[])_data.Clone();
			}
			else {
				return new Span<T>(data).As<byte>().ToArray();
			}
		}

		private static bool Cacheable(Texture2D texture) {
			return texture.LevelCount <= 1;
		}

		private static void SetDataPurge<T>(Texture2D texture, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : unmanaged {
			if (texture is ManagedTexture2D) {
				return;
			}

			if (!ScaledTexture.Validate(texture)) {
				return;
			}

			var byteData = Cacheable(texture) ? GetByteArray(data) : null;
			var elementSize = Marshal.SizeOf(typeof(T));

			ScaledTexture.Purge(
				reference: texture,
				bounds: rect,
				data: new DataRef<byte>(
					data: byteData,
					offset: startIndex * elementSize,
					length: elementCount * elementSize
				)
			);
		}

		[Harmonize("SetData", HarmonizeAttribute.Fixation.Postfix, PriorityLevel.Last, HarmonizeAttribute.Generic.Struct)]
		private static void OnSetDataPost<T> (Texture2D __instance, T[] data) where T : unmanaged {
			SetDataPurge(
				__instance,
				null,
				data,
				0,
				data.Length
			);
		}

		[Harmonize("SetData", HarmonizeAttribute.Fixation.Postfix, PriorityLevel.Last, HarmonizeAttribute.Generic.Struct)]
		private static void OnSetDataPost<T> (Texture2D __instance, T[] data, int startIndex, int elementCount) where T : unmanaged {
			SetDataPurge(
				__instance,
				null,
				data,
				startIndex,
				elementCount
			);
		}

		[Harmonize("SetData", HarmonizeAttribute.Fixation.Postfix, PriorityLevel.Last, HarmonizeAttribute.Generic.Struct)]
		private static void OnSetDataPost<T> (Texture2D __instance, int level, Rectangle? rect, T[] data, int startIndex, int elementCount) where T : unmanaged {
			SetDataPurge(
				__instance,
				rect,
				data,
				startIndex,
				elementCount
			);
		}

		// A horrible, horrible hack to stop a rare-ish crash when zooming or when the device resets. It doesn't appear to originate in SpriteMaster, but SM most certainly
		// makes it worse. This will force the texture to regenerate on the fly if it is in a zombie state.
		[Harmonize("Microsoft.Xna.Framework", "Microsoft.Xna.Framework.Helpers", "CheckDisposed", HarmonizeAttribute.Fixation.Prefix, PriorityLevel.Last, instance: false, platform: HarmonizeAttribute.Platform.Windows)]
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
							new [] {
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
	}
}
