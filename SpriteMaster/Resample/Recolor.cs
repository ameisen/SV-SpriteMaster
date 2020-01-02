using ImageMagick;
using SpriteMaster.Types;
using System.IO;

namespace SpriteMaster.Resample {
	internal static class Recolor {
		internal static unsafe T[] Enhance<T> (T[] data, Vector2I size) where T : unmanaged {
			lock (typeof(Upscaler)) {
				fixed (T* ptr = data) {
					using var bitmapStream = new UnmanagedMemoryStream((byte*)ptr, data.Length * sizeof(T), data.Length * sizeof(T), FileAccess.ReadWrite);
					using (var image = new MagickImage(bitmapStream, new PixelReadSettings(size.Width, size.Height, StorageType.Char, PixelMapping.RGBA))) {
						image.ColorSpace = ImageMagick.ColorSpace.sRGB;
						image.ColorType = ColorType.TrueColorAlpha;

						//image.Depth = 8;
						//image.BitDepth(Channels.Alpha, 8);

						image.HasAlpha = true;

						image.Depth = 8;
						image.BitDepth(Channels.Alpha, 8);

						//image.AutoLevel(Channels.RGB);
						//image.BrightnessContrast(new Percentage(50), new Percentage(50));
						image.Contrast(false);
						//image.Emboss();
						//image.Enhance();
						//image.Equalize();
						//image.GammaCorrect(2.4);
						//image.Normalize();
						//image.AutoGamma();
						//image.RandomThreshold(new Percentage(0), new Percentage(100));
						//image.SelectiveBlur(6.0, 1.0, 1.0);
						image.UnsharpMask(6.0, 1.0);

						//image.Transpose();

						var outputArray = new T[data.Length];
						fixed (T* outPtr = outputArray) {
							using (var outputStraem = new UnmanagedMemoryStream((byte*)outPtr, outputArray.Length * sizeof(T), outputArray.Length * sizeof(T), FileAccess.ReadWrite)) {
								image.Write(outputStraem, MagickFormat.Rgba);
							}
						}
						return outputArray;

						/*
						foreach (int i in 0..bitmapData.Length) {
							unchecked {
								var color = (uint)bitmapData[i];

								// RGBA to ABGR
								color =
									((color >> 24) & 0xFF) |
									((color >> 8) & 0xFF00) |
									((color << 8) & 0xFF0000) |
									((color << 24) & 0xFF000000);

								bitmapData[i] = (int)color;
							}
						}
						*/
					}
				}
			}
		}
	}
}
