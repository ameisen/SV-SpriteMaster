using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Types;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SpriteMaster.Extensions {
	internal static class Texture {
		internal static int Area (this Texture2D texture) {
			return texture.Width * texture.Height;
		}

		internal static Vector2I Extent (this Texture2D texture) {
			return new Vector2I(texture.Width, texture.Height);
		}

		internal static long SizeBytes (this SurfaceFormat format, int texels) {
			switch (format) {
				case SurfaceFormat.Dxt1:
					return texels / 2;
			}

			long elementSize = format switch
			{
				SurfaceFormat.Color => 4,
				SurfaceFormat.Bgr565 => 2,
				SurfaceFormat.Bgra5551 => 2,
				SurfaceFormat.Bgra4444 => 2,
				SurfaceFormat.Dxt3 => 1,
				SurfaceFormat.Dxt5 => 1,
				SurfaceFormat.NormalizedByte2 => 2,
				SurfaceFormat.NormalizedByte4 => 4,
				SurfaceFormat.Rgba1010102 => 4,
				SurfaceFormat.Rg32 => 4,
				SurfaceFormat.Rgba64 => 8,
				SurfaceFormat.Alpha8 => 1,
				SurfaceFormat.Single => 4,
				SurfaceFormat.Vector2 => 8,
				SurfaceFormat.Vector4 => 16,
				SurfaceFormat.HalfSingle => 2,
				SurfaceFormat.HalfVector2 => 4,
				SurfaceFormat.HalfVector4 => 8,
				_ => throw new ArgumentException(nameof(format))
			};

			return (long)texels * elementSize;
		}

		internal static long SizeBytes (this Texture2D texture) {
			return texture.Format.SizeBytes(texture.Area());
		}

		internal static long SizeBytes (this ScaledTexture.ManagedTexture2D texture) {
			return (long)texture.Area() * 4;
		}

		internal static bool IsBlockCompressed (this Texture2D texture) {
			switch (texture.Format) {
				case SurfaceFormat.Dxt1:
				case SurfaceFormat.Dxt3:
				case SurfaceFormat.Dxt5:
					return true;
			}
			return false;
		}

		internal static unsafe void SetDataEx (this Texture2D texture, int[] data) {
			// If we are getting integer data in, we may have to convert it.
			if (texture.IsBlockCompressed()) {
				var byteData = new byte[texture.SizeBytes()];
				fixed (int* source = data) {
					byte* pSource = (byte*)source;
					fixed (byte* dest = byteData) {
						foreach (int i in 0.Until(byteData.Length)) {
							dest[i] = pSource[i];
						}
						//Marshal.Copy(pSource, 0, dest, data.Length);
					}
				}
				texture.SetData(byteData);
			}
			else {
				texture.SetData<int>(data);
			}
		}

		internal static Bitmap Resize (this Bitmap source, Vector2I size, System.Drawing.Drawing2D.InterpolationMode filter = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic, bool discard = true) {
			if (size == new Vector2I(source)) {
				return source;
			}
			var output = new Bitmap(size.Width, size.Height);
			try {
				using (var g = Graphics.FromImage(output)) {
					g.InterpolationMode = filter;
					g.DrawImage(source, 0, 0, output.Width, output.Height);
				}
				if (discard) {
					source.Dispose();
				}
				return output;
			}
			catch {
				output.Dispose();
				throw;
			}
		}

		internal static string SafeName (this Texture2D texture) {
			if (texture.Name != null && texture.Name != "") {
				return texture.Name;
			}

			return "Unknown";
		}

		internal static string SafeName (this ScaledTexture texture) {
			if (texture.Name != null && texture.Name != "") {
				return texture.Name;
			}

			return "Unknown";
		}
	}
}
