using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SpriteMaster.Resample;

static class TextureDecode {
	private static class Internal {
		internal static Span<byte> Decode(Texture2D texture, ReadOnlySpan<byte> data) {
			return BlockCompress.Decompress(data, texture.Extent(), texture.Format);
		}
	}

	private static class Graphics {
		internal static Span<byte> Decode(Texture2D texture, ReadOnlySpan<byte> data) {
			using (var tempTexture = new Texture2D(DrawState.Device, texture.Width, texture.Height, false, texture.Format)) {
				tempTexture.SetData(data.ToArray());
				using var pngStream = new MemoryStream();
				tempTexture.SaveAsPng(pngStream, tempTexture.Width, tempTexture.Height);
				//using (var fileStream = new FileStream("D:\\font.png", FileMode.Create)) {
				//	tempTexture.SaveAsPng(fileStream, tempTexture.Width, tempTexture.Height);
				//}
				pngStream.Flush();
				using var pngTexture = Texture2D.FromStream(DrawState.Device, pngStream);
				byte[] resultData = GC.AllocateUninitializedArray<byte>(pngTexture.Width * pngTexture.Height * sizeof(int), pinned: true);
				pngTexture.GetData(resultData);

				return resultData.AsSpan();
			}
		}
	}

	private static class MonoXNA {
		private static readonly Type DxtUtil = typeof(Microsoft.Xna.Framework.Graphics.Texture2D).Assembly.GetType("DxtUtil");

		internal delegate byte[] DecompressDelegateArray(byte[] data, int width, int height);
		internal delegate byte[] DecompressDelegateStream(Stream data, int width, int height);

		internal static readonly DecompressDelegateArray DecompressDXT1Array = GetDelegate<DecompressDelegateArray>("DecompressDxt1");
		internal static readonly DecompressDelegateStream DecompressDXT1Stream = GetDelegate<DecompressDelegateStream>("DecompressDxt1");

		internal static readonly DecompressDelegateArray DecompressDXT3Array = GetDelegate<DecompressDelegateArray>("DecompressDxt3");
		internal static readonly DecompressDelegateStream DecompressDXT3Stream = GetDelegate<DecompressDelegateStream>("DecompressDxt3");

		internal static readonly DecompressDelegateArray DecompressDXT5Array = GetDelegate<DecompressDelegateArray>("DecompressDxt5");
		internal static readonly DecompressDelegateStream DecompressDXT5Stream = GetDelegate<DecompressDelegateStream>("DecompressDxt5");

		private static T GetDelegate<T>(string name) where T : Delegate => DxtUtil.GetMethod(
			name,
			BindingFlags.NonPublic | BindingFlags.Static,
			null,
			new Type[] { typeof(T) == typeof(DecompressDelegateArray) ? typeof(byte[]) : typeof(Stream), typeof(int), typeof(int) },
			null
		).CreateDelegate<T>();

		internal static Span<byte> Decode(Texture2D texture, ReadOnlySpan<byte> data) => Decode(texture.Format, (texture.Width, texture.Height), data);

		internal static Span<byte> Decode(SurfaceFormat format, Vector2I size, ReadOnlySpan<byte> data) {
			switch (format) {
				case SurfaceFormat.Dxt1:
				case SurfaceFormat.Dxt1SRgb:
				case SurfaceFormat.Dxt1a:
					return DecompressDXT1Array(data.ToArray(), size.Width, size.Height);
				case SurfaceFormat.Dxt3:
				case SurfaceFormat.Dxt3SRgb:
					return DecompressDXT3Array(data.ToArray(), size.Width, size.Height);
				case SurfaceFormat.Dxt5:
				case SurfaceFormat.Dxt5SRgb:
					return DecompressDXT5Array(data.ToArray(), size.Width, size.Height);
				default:
					return default;
			}
		}
	}

	internal static Span<byte> DecodeBlockCompressedTexture(Texture2D texture, ReadOnlySpan<byte> data) {
		// return Internal.Decode(texture, data);

		// return Graphics.Decode(texture, data);

		return MonoXNA.Decode(texture, data);
	}

	internal static Span<byte> DecodeBlockCompressedTexture(SurfaceFormat format, Vector2I size, ReadOnlySpan<byte> data) {
		// return Internal.Decode(texture, data);

		// return Graphics.Decode(texture, data);

		return MonoXNA.Decode(format, size, data);
	}
}
