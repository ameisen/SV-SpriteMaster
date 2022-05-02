using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;
using System.IO;

namespace SpriteMaster.Resample.Decoder;

static class GraphicsBlockDecoder {
	internal static Span<byte> Decode(ReadOnlySpan<byte> data, Vector2I size, SurfaceFormat format) {
		using (var tempTexture = new DecodingTexture2D(DrawState.Device, size.Width, size.Height, false, format) { Name = "Decode Texture" }) {
			tempTexture.SetData(data.ToArray());
			using var pngStream = new MemoryStream();
			tempTexture.SaveAsPng(pngStream, tempTexture.Width, tempTexture.Height);
			pngStream.Flush();
			using var pngTexture = XTexture2D.FromStream(DrawState.Device, pngStream);
			var resultData = GC.AllocateUninitializedArray<byte>(pngTexture.Area() * sizeof(uint), pinned: true);
			pngTexture.GetData(resultData);

			return resultData.AsSpan();
		}
	}
}
