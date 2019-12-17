using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Types;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using xBRZNet2;
using static SpriteMaster.ScaledTexture;

namespace SpriteMaster {
	internal sealed class Upscaler {
		static Bitmap CreateBitmap (in int[] source, in Vector2I size, PixelFormat format = PixelFormat.Format32bppArgb) {
			var newImage = new Bitmap(size.Width, size.Height, format);
			var rectangle = new Bounds(newImage);
			var newBitmapData = newImage.LockBits(rectangle, ImageLockMode.WriteOnly, format);
			// Get the address of the first line.
			var newBitmapPointer = newBitmapData.Scan0;
			//http://stackoverflow.com/a/1917036/294804
			// Copy the RGB values back to the bitmap

			bool hasPadding = newBitmapData.Stride != (newBitmapData.Width * sizeof(int));

			// Handle stride correctly as input data does not have any stride?
			const bool CopyWithPadding = true;
			if (CopyWithPadding && hasPadding) {
				var rowElements = newImage.Width;
				var rowSize = newBitmapData.Stride;

				int sourceOffset = 0;
				foreach (int row in 0.Until(newImage.Height)) {
					Marshal.Copy(source, sourceOffset, newBitmapPointer, rowElements);
					sourceOffset += rowElements;
					newBitmapPointer += rowSize;
				}
			}
			else {
				var intCount = newBitmapData.Stride * newImage.Height / sizeof(int);
				Marshal.Copy(source, 0, newBitmapPointer, intCount);
			}
			// Unlock the bits.
			newImage.UnlockBits(newBitmapData);

			return newImage;
		}

		private static readonly string TextureCacheName = "TextureCache";
		private static readonly string JunctionCacheName = $"{TextureCacheName}_Current";
		private static readonly string CacheName = $"{TextureCacheName}_{typeof(Upscaler).Assembly.GetName().Version.ToString()}";
		private static readonly string LocalDataPath = Path.Combine(Config.LocalRoot, CacheName);
		private static readonly string DumpPath = Path.Combine(LocalDataPath, "dump");

		enum LinkType : int {
			File = 0,
			Directory = 1
		}

		[DllImport("kernel32.dll")]
		static extern bool CreateSymbolicLink (string Link, string Target, LinkType Type);

		private static bool IsSymbolic (string path) {
			FileInfo pathInfo = new FileInfo(path);
			return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
		}

		static Upscaler () {
			// Delete any old caches.
			try {
				foreach (var root in new string[] { Config.LocalRoot }) {
					var directories = Directory.EnumerateDirectories(root);
					foreach (var directory in directories) {
						try {
							if (!Directory.Exists(directory)) {
								continue;
							}
							if (IsSymbolic(directory)) {
								continue;
							}
							var endPath = Path.GetFileName(directory);
							if (endPath != CacheName && endPath != JunctionCacheName) {
								// If it doesn't match, it's outdated and should be deleted.
								Directory.Delete(directory, true);
							}
						}
						catch { /* Ignore failures */ }
					}
				}
			}
			catch { /* Ignore failures */ }

			if (Config.Cache.Enabled) {
				// Create the directory path
				Directory.CreateDirectory(LocalDataPath);

				// Mark the directory as compressed because this is very space wasteful and we are currently not performing compression.
				// https://stackoverflow.com/questions/624125/compress-a-folder-using-ntfs-compression-in-net
				try {
					var dir = new DirectoryInfo(LocalDataPath);
					if ((dir.Attributes & FileAttributes.Compressed) == 0) {
						var objectPath = $"Win32_Directory.Name='{dir.FullName.Replace("\\", @"\\").TrimEnd('\\')}'";
						using (ManagementObject obj = new ManagementObject(objectPath)) {
							using (obj.InvokeMethod("Compress", null, null)) {
								// I don't really care about the return value, 
								// if we enabled it great but it can also be done manually
								// if really needed
							}
						}
					}
				}
				catch { /* Ignore failures */ }
			}

			Directory.CreateDirectory(LocalDataPath);
			if (Config.Debug.Sprite.DumpReference || Config.Debug.Sprite.DumpResample) {
				Directory.CreateDirectory(DumpPath);
			}

			// Set up a symbolic link to aid in debugging.
			try {
				Directory.Delete(Path.Combine(Config.LocalRoot, JunctionCacheName), false);
			}
			catch { /* Ignore failure */ }
			try {
				CreateSymbolicLink(
					Link: Path.Combine(Config.LocalRoot, JunctionCacheName),
					Target: Path.Combine(LocalDataPath),
					Type: LinkType.Directory
				);
			}
			catch { /* Ignore failure */ }
		}

		private class MetaData {
			internal readonly ulong Hash;

			internal MetaData (ulong hash) {
				this.Hash = hash;
			}
		}

		private static readonly ConditionalWeakTable<Texture2D, MetaData> MetaCache = new ConditionalWeakTable<Texture2D, MetaData>();
		//private static Dictionary<ulong, Texture2D> TextureCache = new Dictionary<ulong, Texture2D>();

		internal static ulong GetHash (in TextureWrapper input, bool desprite) {
			ulong hash;
			lock (MetaCache) {
				if (MetaCache.TryGetValue(input.Reference, out var metaData)) {
					hash = metaData.Hash;
				}
				else {
					hash = input.Hash();
					MetaCache.Add(input.Reference, new MetaData(hash: hash));
				}
			}
			if (desprite) {
				hash ^= input.IndexRectangle.Hash();
			}
			return hash;
		}

		internal static class ColorSpace {
			// TODO : After conversion, we should be temporarily working with 64-bit textures instead of 32-bit. We want to retain precision.

			private const bool UseStrictConversion = true;
			private const bool IgnoreAlpha = true; // sRGB Alpha doesn't make much sense. I suppose it isn't impossible, but why? It's a data channel.
			private const double ByteMax = (double)byte.MaxValue;
			private const double Gamma = 2.4;

			// https://entropymine.com/imageworsener/srgbformula/
			private static double ToSRGB (in double value, in double gamma) {
				if (UseStrictConversion) {
					if (value <= 0.0031308)
						return value * 12.92;
					else
						return 1.055 * Math.Pow(value, 1.0 / gamma) - 0.055;
				}
				else {
					return Math.Pow(value, gamma);
				}
			}

			private static double ToLinear (in double value, in double gamma) {
				if (UseStrictConversion) {
					if (value <= 0.04045)
						return value / 12.92;
					else
						return Math.Pow((value + 0.055) / 1.055, gamma);
				}
				else {
					return Math.Pow(value, 1.0 / gamma);
				}
			}

			internal static void ConvertSRGBToLinear (in int[] textureData, in Texel.Ordering order = Texel.Ordering.ABGR, in double gamma = Gamma) {
				ConvertSRGBToLinear(textureData.AsSpan(), order, gamma);
			}

			internal static void ConvertSRGBToLinear (in Span<int> textureData, in Texel.Ordering order = Texel.Ordering.ABGR, in double gamma = Gamma) {
				foreach (int i in 0.Until(textureData.Length)) {
					ref var texelValue = ref textureData[i];

					var texel = Texel.From(texelValue, order);
					var R = (double)texel.R / ByteMax;
					var G = (double)texel.G / ByteMax;
					var B = (double)texel.B / ByteMax;

					Contract.AssertLessEqual(R, 1.0);
					Contract.AssertLessEqual(G, 1.0);
					Contract.AssertLessEqual(B, 1.0);

					if (!IgnoreAlpha) {
						var A = (double)texel.A / ByteMax;
						A = ToLinear(A, gamma);
						texel.A = unchecked((byte)(A * ByteMax).RoundToInt());
					}
					R = ToLinear(R, gamma);
					G = ToLinear(G, gamma);
					B = ToLinear(B, gamma);

					texel.R = unchecked((byte)(R * ByteMax).RoundToInt());
					texel.G = unchecked((byte)(G * ByteMax).RoundToInt());
					texel.B = unchecked((byte)(B * ByteMax).RoundToInt());

					texelValue = texel.To(order);
				}
			}

			internal static void ConvertLinearToSRGB (in int[] textureData, in Texel.Ordering order = Texel.Ordering.ABGR, in double gamma = Gamma) {
				ConvertLinearToSRGB(textureData.AsSpan(), order, gamma);
			}

			internal static void ConvertLinearToSRGB (in Span<int> textureData, in Texel.Ordering order = Texel.Ordering.ABGR, in double gamma = Gamma) {
				foreach (int i in 0.Until(textureData.Length)) {
					ref var texelValue = ref textureData[i];

					var texel = Texel.From(texelValue, order);
					var R = (double)texel.R / ByteMax;
					var G = (double)texel.G / ByteMax;
					var B = (double)texel.B / ByteMax;

					Contract.AssertLessEqual(R, 1.0);
					Contract.AssertLessEqual(G, 1.0);
					Contract.AssertLessEqual(B, 1.0);

					if (!IgnoreAlpha) {
						var A = (double)texel.A / ByteMax;
						A = ToSRGB(A, gamma);
						texel.A = unchecked((byte)(A * ByteMax).RoundToInt());
					}
					R = ToSRGB(R, gamma);
					G = ToSRGB(G, gamma);
					B = ToSRGB(B, gamma);

					texel.R = unchecked((byte)(R * ByteMax).RoundToInt());
					texel.G = unchecked((byte)(G * ByteMax).RoundToInt());
					texel.B = unchecked((byte)(B * ByteMax).RoundToInt());

					texelValue = texel.To(order);
				}
			}
		}

		internal static Texture2D Upscale (ScaledTexture texture, ref int scale, in TextureWrapper input, bool desprite, ulong hash, ref Vector2B wrapped, bool allowPadding) {
			wrapped.Set(false);

			var output = input.Reference;

			var inputSize = desprite ? input.Size.Extent : input.ReferenceSize;

			if (Config.Resample.SmartScale && Config.Resample.Scale) {
				foreach (int s in Config.Resample.MaxScale.Until(scale)) {
					var newDimensions = inputSize * s;
					if (newDimensions.X <= Config.ClampDimension && newDimensions.Y <= Config.ClampDimension) {
						scale = s;
						break;
					}
				}
			}

			var scaledSize = inputSize * scale;
			var newSize = scaledSize.Min(Config.ClampDimension);

			if (Config.RestrictSize) {
				var scaledTestSize = inputSize * scale;
				if (scaledTestSize.Width > Config.ClampDimension) {
					newSize.Width = input.ReferenceSize.Width;
				}
				if (scaledTestSize.Height > Config.ClampDimension) {
					newSize.Height = input.ReferenceSize.Height;
				}
			}

			var scaledDimensions = input.Size.Extent * scale;
			texture.UnpaddedSize = newSize;

			/*
      if (TextureCache.TryGetValue(hash, out Texture2D cachedTexture))
      {
        if (!cachedTexture.IsDisposed)
        {
          Debug.ErrorLn("Found Cached Texture");
          return cachedTexture;
        }
      }
      */

			// Apparently, using the filename is not sufficient for hashing cache data. So, we will need to hash the texture data, apparently. This is really messy.
			var hashString = hash.ToString("x");
			var localDataPath = Path.Combine(LocalDataPath, $"{hashString}.cache");

			int[] bitmapData = null;
			try {
				if (Config.Cache.Enabled && File.Exists(localDataPath)) {
					int retries = Config.Cache.LockRetries;
					bool success = false;

					while (!success && retries-- > 0) {
						if (File.Exists(localDataPath)) {
							// https://stackoverflow.com/questions/1304/how-to-check-for-file-lock
							bool WasLocked (in IOException ex) {
								var errorCode = Marshal.GetHRForException(ex) & ((1 << 16) - 1);
								return errorCode == 32 || errorCode == 33;
							}

							try {
								using (var reader = new BinaryReader(new FileStream(localDataPath, FileMode.Open, FileAccess.Read, FileShare.Read))) {
									wrapped.X = reader.ReadBoolean();
									wrapped.Y = reader.ReadBoolean();
									texture.Padding.X = reader.ReadInt32();
									texture.Padding.Y = reader.ReadInt32();

									var remainingSize = reader.BaseStream.Length - reader.BaseStream.Position;
									bitmapData = new int[remainingSize / sizeof(int)];

									foreach (int i in 0.Until(bitmapData.Length)) {
										bitmapData[i] = reader.ReadInt32();
									}

									if (!texture.Padding.IsEmpty) {
										var paddedSize = scaledSize + texture.Padding * 2;
										scaledDimensions = newSize = paddedSize;
										scaledSize = input.ReferenceSize * scale;

										scaledDimensions = newSize = scaledSize = paddedSize * scale;
									}
								}
								success = true;
							}
							catch (IOException ex) {
								bool wasLocked = WasLocked(ex);
								if (WasLocked(ex)) {
									Debug.InfoLn($"File was locked when trying to load cache file '{hashString}': {ex.Message} [{retries} retries]");
									Thread.Sleep(Config.Cache.LockSleepMS);
								}
								else {
									Debug.WarningLn($"IOException when trying to load cache file '{hashString}': {ex.Message}");
									retries = 0;
								}
							}
						}
					}
				}

				if (bitmapData == null) {
					Bitmap GetDumpBitmap (Bitmap source) {
						var dump = (Bitmap)source.Clone();
						foreach (int y in 0.Until(dump.Height))
							foreach (int x in 0.Until(dump.Width)) {
								dump.SetPixel(
									x, y,
									Texel.FromARGB(dump.GetPixel(x, y)).Color
								);
							}

						return dump;
					}

					wrapped = input.Wrapped;

					Vector2B WrappedX = new Vector2B(wrapped.X);
					Vector2B WrappedY = new Vector2B(wrapped.Y);

					unsafe {
						using (var rawDataHandle = input.Data.AsMemory().Pin()) {
							var rawData = new Span<int>(rawDataHandle.Pointer, input.Data.Length / sizeof(int));

							if (Config.Resample.DeSprite && Config.WrapDetection.Enabled && Config.Resample.EnableWrappedAddressing) {
								byte GetAlpha (in int sample) {
									return unchecked((byte)(((uint)sample >> 24) & 0xFF));
								}

								var rawInputSize = input.ReferenceSize;
								var spriteInputSize = input.Size;

								long numSamples = 0;
								double meanAlphaF = 0.0f;
								if (!wrapped.X || !wrapped.Y) {
									foreach (int y in 0.Until(spriteInputSize.Height)) {
										int offset = (y + spriteInputSize.Top) * rawInputSize.Width + spriteInputSize.Left;
										foreach (int x in 0.Until((spriteInputSize.Width))) {
											int address = offset + x;
											int sample = rawData[address];
											meanAlphaF += GetAlpha(sample);
											++numSamples;
										}
									}
								}
								//meanAlphaF /= numSamples;
								//meanAlphaF *= (double)Config.WrapDetection.alphaThreshold / 255.0;
								byte alphaThreshold = Config.WrapDetection.alphaThreshold; //(byte)Math.Min(meanAlphaF.RoundToInt(), byte.MaxValue);

								// Count the fragments that are not alphad out completely on the edges.
								// Both edges must meet the threshold.
								if (!wrapped.X) {
									var samples = stackalloc int[] { 0, 0 };
									foreach (int y in 0.Until(spriteInputSize.Height)) {
										int offset = (y + spriteInputSize.Top) * rawInputSize.Width + spriteInputSize.Left;
										int sample0 = rawData[offset];
										int sample1 = rawData[offset + (spriteInputSize.Width - 1)];

										if (GetAlpha(sample0) >= alphaThreshold) {
											samples[0]++;
										}
										if (GetAlpha(sample1) >= alphaThreshold) {
											samples[1]++;
										}
									}
									int threshold = ((float)inputSize.Height * Config.WrapDetection.edgeThreshold).RoundToInt();
									WrappedX.Negative = samples[0] >= threshold;
									WrappedX.Positive = samples[1] >= threshold;
									wrapped.X = WrappedX[0] && WrappedX[1];
								}
								if (!wrapped.Y) {
									var samples = stackalloc int[] { 0, 0 };
									var offsets = stackalloc int[] { spriteInputSize.Top * rawInputSize.Width, (spriteInputSize.Bottom - 1) * rawInputSize.Width};
									int sampler = 0;
									foreach (int i in 0.Until(2)) {
										var yOffset = offsets[i];
										foreach (int x in 0.Until(spriteInputSize.Width)) {
											int offset = yOffset + x + spriteInputSize.Left;
											int sample = rawData[offset];
											if (GetAlpha(sample) >= alphaThreshold) {
												samples[sampler]++;
											}
										}
										sampler++;
									}
									int threshold = ((float)inputSize.Width * Config.WrapDetection.edgeThreshold).RoundToInt();
									WrappedY.Negative = samples[0] >= threshold;
									WrappedY.Positive = samples[0] >= threshold;
									wrapped.Y = WrappedY[0] && WrappedY[1];
								}
							}

							wrapped = (wrapped & Config.Resample.EnableWrappedAddressing);

							if (Config.Debug.Sprite.DumpReference) {
								using (var filtered = CreateBitmap(rawData.ToArray(), input.ReferenceSize, PixelFormat.Format32bppArgb)) {
									using (var submap = (Bitmap)filtered.Clone(input.Size, filtered.PixelFormat)) {
										var dump = GetDumpBitmap(submap);
										var path = Path.Combine(DumpPath, $"{input.Reference.SafeName().Replace("\\", ".")}.{hashString}.reference.png");
										File.Delete(path);
										dump.Save(path, ImageFormat.Png);
									}
								}
							}

							if (Config.Resample.Smoothing) {
								var scalerConfig = new xBRZNet2.Config(wrappedX: wrapped.X, wrappedY: wrapped.Y);

								// Do we need to pad the sprite?
								var prescaleData = rawData;
								var prescaleSize = input.ReferenceSize;

								var shouldPad = new Vector2B(
									!(WrappedX.Positive && WrappedX.Negative) && Config.Resample.Padding.Enabled && allowPadding,
									!(WrappedY.Positive && WrappedX.Negative) && Config.Resample.Padding.Enabled && allowPadding
								);

								bool padded = false;

								if (
									(
										prescaleSize.X <= Config.Resample.Padding.MinSize &&
										prescaleSize.Y <= Config.Resample.Padding.MinSize
									) ||
									Config.Resample.Padding.IgnoreUnknown && (input.Reference.Name == null || input.Reference.Name == "")
								) {
									shouldPad = Vector2B.False;
								}

								if (shouldPad.X || shouldPad.Y) {
									int padding = scale;
									int scaledPadding = padding * scale;

									// TODO we only need to pad the edge that has texels. Double padding is wasteful.
									var paddedSize = inputSize.Clone();
									var spriteSize = inputSize.Clone();


									if (shouldPad.X) {
										if ((paddedSize.X + padding * 2) * scale > Config.ClampDimension) {
											shouldPad.X = false;
										}
										else {
											paddedSize.X += padding * 2;
											texture.Padding.X = padding;
										}
									}
									if (shouldPad.Y) {
										if ((paddedSize.Y + padding * 2) * scale > Config.ClampDimension) {
											shouldPad.Y = false;
										}
										else {
											paddedSize.Y += padding * 2;
											texture.Padding.Y = padding;
										}
									}


									if (shouldPad.X || shouldPad.Y) {
										padded = true;

										int[] paddedData = new int[paddedSize.Area];

										int y = 0;

										// TODO : when writing padding, we might want to consider color clamping but without alpha.

										const int padConstant = 0x00000000;

										void WritePaddingY () {
											if (!shouldPad.Y)
												return;
											foreach (int i in 0.Until(padding)) {
												int strideOffset = y * paddedSize.Width;
												foreach (int x in 0.Until(paddedSize.Width)) {
													paddedData[strideOffset + x] = padConstant;
												}
												++y;
											}
										}

										WritePaddingY();

										foreach (int i in 0.Until(spriteSize.Height)) {
											int strideOffset = y * paddedSize.Width;
											int strideOffsetRaw = (i + input.Size.Top) * prescaleSize.Width;
											// Write a padded X line
											int xOffset = strideOffset;
											void WritePaddingX() {
												if (!shouldPad.X)
													return;
												foreach (int x in 0.Until(padding)) {
													paddedData[xOffset++] = padConstant;
												}
											}
											WritePaddingX();
											foreach (int x in 0.Until(spriteSize.Width)) {
												paddedData[xOffset++] = rawData[strideOffsetRaw + x + input.Size.Left];
											}
											WritePaddingX();
											++y;
										}

										WritePaddingY();

										prescaleData = new Span<int>(paddedData);
										prescaleSize = paddedSize;
										scaledDimensions = newSize = scaledSize = prescaleSize * scale;
									}
								}

								bitmapData = new int[scaledSize.Area];

								try {
									new xBRZScaler(
										scaleMultiplier: scale,
										sourceData: prescaleData,
										sourceWidth: prescaleSize.Width,
										sourceHeight: prescaleSize.Height,
										sourceTarget: padded ? new Bounds(prescaleSize) : input.Size,
										targetData: bitmapData,
										configuration: scalerConfig
									);
								}
								catch (Exception ex) {
									Debug.ErrorLn($"There was an exception in the upscaler: {ex.Message}");
									Debug.ErrorLn(ex.StackTrace);
									throw;
								}
								//ColorSpace.ConvertLinearToSRGB(bitmapData, Texel.Ordering.ARGB);
							}
							else {
								bitmapData = rawData.ToArray();
							}
						}
					}

					if (Config.Debug.Sprite.DumpResample) {
						using (var filtered = CreateBitmap(bitmapData, scaledDimensions, PixelFormat.Format32bppArgb)) {
							var dump = GetDumpBitmap(filtered);
							var path = Path.Combine(DumpPath, $"{input.Reference.SafeName().Replace("\\", ".")}.{hashString}.resample-{WrappedX}-{WrappedY}-{texture.Padding.X}-{texture.Padding.Y}.png");
							File.Delete(path);
							dump.Save(path, ImageFormat.Png);
						}
					}

					if (scaledDimensions.Width != newSize.Width || scaledDimensions.Height != newSize.Height) {
						// This should be incredibly rare - we very rarely need to scale back down.
						using (var filtered = CreateBitmap(bitmapData, scaledDimensions, PixelFormat.Format32bppArgb)) {
							var resized = filtered.Resize(newSize, System.Drawing.Drawing2D.InterpolationMode.Bicubic);
							var resizedData = resized.LockBits(new Bounds(resized), ImageLockMode.ReadOnly, filtered.PixelFormat);
							bitmapData = new int[resized.Width * resized.Height];

							try {
								var dataSize = resizedData.Stride * resizedData.Height;
								var dataPtr = resizedData.Scan0;
								var widthSize = resizedData.Width * sizeof(int);

								var dataBytes = new byte[dataSize];
								int offsetSource = 0;
								int offsetDest = 0;
								foreach (int y in 0.Until(resizedData.Height)) {
									Marshal.Copy(dataPtr + offsetSource, bitmapData, offsetDest, widthSize);
									offsetSource += resizedData.Stride;
									offsetDest += widthSize;
								}
							}
							finally {
								resized.UnlockBits(resizedData);
							}

						}
					}

					if (Config.Cache.Enabled) {
						try {
							using (var writer = new BinaryWriter(File.OpenWrite(localDataPath))) {
								writer.Write(wrapped.X);
								writer.Write(wrapped.Y);
								writer.Write(texture.Padding.X);
								writer.Write(texture.Padding.Y);

								foreach (var v in bitmapData) {
									writer.Write(v);
								}
							}
						}
						catch { }
					}
				}

				if (Config.AsyncScaling.Enabled) {
					var reference = input.Reference;
					Action asyncCall = () => {
						if (reference.IsDisposed) {
							return;
						}
						var newTexture = new ManagedTexture2D(texture, reference, newSize, SurfaceFormat.Color);
						newTexture.Name = reference.SafeName() + " [RESAMPLED]";
						try {
							newTexture.SetData(bitmapData);
							texture.Texture = newTexture;
							texture.Finish();
						}
						catch (Exception ex) {
							Debug.ErrorLn($"There was an exception creating the texture: {ex.Message}");
							Debug.ErrorLn(ex.StackTrace);
							newTexture.Dispose();
							texture.Destroy(reference);
						}
					};
					ScaledTexture.AddPendingAction(asyncCall);
					return null;
				}
				else {
					var newTexture = new ManagedTexture2D(texture, input.Reference, newSize, SurfaceFormat.Color);
					newTexture.Name = input.Reference.SafeName() + " [RESAMPLED]";
					try {
						newTexture.SetData(bitmapData);
						output = newTexture;
					}
					catch (Exception ex) {
						Debug.ErrorLn($"There was an exception in the upscaler: {ex.Message}");
						Debug.ErrorLn(ex.StackTrace);
						newTexture.Dispose();
					}
				}
			}
			catch (Exception ex) {
				Debug.ErrorLn($"An exception was caught during texture processing: {ex.Message}");
				Debug.ErrorLn(ex.GetStackTrace());
			}

			//TextureCache.Add(hash, output);
			return output;
		}
	}
}
