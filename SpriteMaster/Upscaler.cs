//using ManagedSquish;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Resample;
using SpriteMaster.Types;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TeximpNet.Compression;
using TeximpNet.DDS;
using xBRZNet2;
using static SpriteMaster.ScaledTexture;

namespace SpriteMaster {
	internal sealed class Upscaler {
		private class MetaData {
			internal readonly ulong Hash;

			internal MetaData (ulong hash) {
				this.Hash = hash;
			}
		}

		private static readonly ConditionalWeakTable<Texture2D, MetaData> MetaCache = new ConditionalWeakTable<Texture2D, MetaData>();

		internal static void PurgeHash (Texture2D reference) {
			try {
				MetaCache.Remove(reference);
			}
			catch { /* do nothing */ }
		}

		internal static ulong GetHash (TextureWrapper input, bool desprite) {
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

		// TODO : Detangle this method.
		private static long TotalAdditionalSize = 0;

		private static ConditionalWeakTable<Texture2D, object> GCAccount = new ConditionalWeakTable<Texture2D, object>();

		internal static ManagedTexture2D Upscale (ScaledTexture texture, ref int scale, TextureWrapper input, bool desprite, ulong hash, ref Vector2B wrapped, bool allowPadding, bool async) {
			// Try to process the texture twice. Garbage collect after a failure, maybe it'll work then.
			foreach (var _ in 0.To(1)) {
				try {
					return UpscaleInternal(
						texture: texture,
						scale: ref scale,
						input: input,
						desprite: desprite,
						hash: hash,
						wrapped: ref wrapped,
						allowPadding: allowPadding,
						async: async
					);
				}
				catch (OutOfMemoryException) {
					Garbage.Collect(compact: true);
				}
			}

			texture.Texture = null;
			return null;
		}

		private static unsafe ManagedTexture2D UpscaleInternal (ScaledTexture texture, ref int scale, TextureWrapper input, bool desprite, ulong hash, ref Vector2B wrapped, bool allowPadding, bool async) {
			if (TotalAdditionalSize >= Config.ForceGarbageCollectAfter) {
				Debug.InfoLn("Forcing Garbage Compaction");
				Garbage.MarkCompact();
				//Garbage.Collect(true);
				TotalAdditionalSize %= Config.ForceGarbageCollectAfter;
			}

			var rawTextureData = input.Data;
			var spriteFormat = TextureFormat.Color;

			if (Config.GarbageCollectAccountUnownedTextures) {
				if (!GCAccount.TryGetValue(input.Reference, out object v)) {
					GCAccount.Add(input.Reference, null);
					long size = input.Reference.SizeBytes();
					GC.AddMemoryPressure(size);
					input.Reference.Disposing += (object obj, EventArgs args) => {
						long size = ((Texture2D)obj).Area() * sizeof(int);
						GC.RemoveMemoryPressure(size);
					};
				}
			}

			wrapped.Set(false);

			ManagedTexture2D output = null;

			var inputSize = desprite ? input.Size.Extent : input.ReferenceSize;

			if (Config.Resample.SmartScale && Config.Resample.Scale) {
				foreach (int s in Config.Resample.MaxScale.Until(scale)) {
					var newDimensions = inputSize * s;
					if (newDimensions.X <= Config.PreferredMaxTextureDimension && newDimensions.Y <= Config.PreferredMaxTextureDimension) {
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
					newSize.Width = inputSize.Width;
				}
				if (scaledTestSize.Height > Config.ClampDimension) {
					newSize.Height = inputSize.Height;
				}
			}

			var scaledDimensions = input.Size.Extent * scale; // TODO : should this be (inputSize * scale)?
			texture.UnpaddedSize = newSize;

			texture.AdjustedScale = (Vector2)newSize / inputSize;

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
			var cachePath = Cache.GetPath($"{hashString}.cache");

			int[] bitmapData = null;
			try {
				if (Cache.Fetch(cachePath, out var fetchSize, out spriteFormat, out wrapped, out texture.Padding, out texture.BlockPadding, out bitmapData)) {
					scaledDimensions = newSize = scaledSize = fetchSize;
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


					Vector2B WrappedX;
					Vector2B WrappedY;

					var rawData = MemoryMarshal.Cast<byte, int>(rawTextureData.AsSpan());

					var edgeResults = Edge.AnalyzeLegacy(
						data: rawData,
						rawSize: input.ReferenceSize,
						spriteSize: input.Size,
						Wrapped: input.Wrapped
					);

					WrappedX = edgeResults.WrappedX;
					WrappedY = edgeResults.WrappedY;
					wrapped = edgeResults.Wrapped;

					wrapped = (wrapped & Config.Resample.EnableWrappedAddressing);

					if (Config.Debug.Sprite.DumpReference) {
						using (var filtered = Textures.CreateBitmap(rawData.ToArray(), input.ReferenceSize, PixelFormat.Format32bppArgb)) {
							using (var submap = (Bitmap)filtered.Clone(input.Size, filtered.PixelFormat)) {
								var dump = GetDumpBitmap(submap);
								var path = Cache.GetDumpPath($"{input.Reference.SafeName().Replace("\\", ".")}.{hashString}.reference.png");
								File.Delete(path);
								dump.Save(path, System.Drawing.Imaging.ImageFormat.Png);
							}
						}
					}

					if (Config.Resample.Smoothing) {
						var scalerConfig = new xBRZNet2.Config(wrappedX: wrapped.X && false, wrappedY: wrapped.Y && false);

						// Do we need to pad the sprite?
						var prescaleData = rawData;
						var prescaleSize = input.ReferenceSize;

						var shouldPad = new Vector2B(
							!(WrappedX.Positive || WrappedX.Negative) && Config.Resample.Padding.Enabled && allowPadding && inputSize.X > 1,
							!(WrappedY.Positive || WrappedX.Negative) && Config.Resample.Padding.Enabled && allowPadding && inputSize.Y > 1
						);

						var outputSize = input.Size;

						if (
							(
								prescaleSize.X <= Config.Resample.Padding.MinimumSizeTexels &&
								prescaleSize.Y <= Config.Resample.Padding.MinimumSizeTexels
							) ||
							Config.Resample.Padding.IgnoreUnknown && (input.Reference.Name == null || input.Reference.Name == "")
						) {
							shouldPad = Vector2B.False;
						}

						var requireBlockPadding = new Vector2B(
							inputSize.X >= 4 && !BlockCompress.IsBlockMultiple(inputSize.X),
							inputSize.Y >= 4 && !BlockCompress.IsBlockMultiple(inputSize.Y)
						) & Config.Resample.UseBlockCompression;

						if ((shouldPad | requireBlockPadding).Any) {
							int expectedPadding = Math.Max(1, scale / 2);

							// TODO we only need to pad the edge that has texels. Double padding is wasteful.
							var paddedSize = inputSize.Clone();
							var spriteSize = inputSize.Clone();

							if (shouldPad.X) {
								if ((paddedSize.X + expectedPadding * 2) * scale > Config.ClampDimension) {
									shouldPad.X = false;
								}
								else {
									paddedSize.X += expectedPadding * 2;
									texture.Padding.X = expectedPadding;
								}
							}
							if (shouldPad.Y) {
								if ((paddedSize.Y + expectedPadding * 2) * scale > Config.ClampDimension) {
									shouldPad.Y = false;
								}
								else {
									paddedSize.Y += expectedPadding * 2;
									texture.Padding.Y = expectedPadding;
								}
							}

							requireBlockPadding = new Vector2B(
								inputSize.X >= 4 && !BlockCompress.IsBlockMultiple(paddedSize.X),
								inputSize.Y >= 4 && !BlockCompress.IsBlockMultiple(paddedSize.Y)
							) & Config.Resample.UseBlockCompression;

							// Block padding can never take us over a dimension clamp, as the clamps themselves are multiples of the block padding (4, generally).
							if (requireBlockPadding.X) {
								texture.BlockPadding.X = 4 - paddedSize.X % 4;
							}
							if (requireBlockPadding.Y) {
								texture.BlockPadding.Y = 4 - paddedSize.Y % 4;
							}

							paddedSize += texture.BlockPadding;

							var hasPadding = shouldPad | requireBlockPadding;

							if (hasPadding.Any) {
								int[] paddedData = new int[paddedSize.Area];

								int y = 0;

								// TODO : when writing padding, we might want to consider color clamping but without alpha, especially for block padding.

								const int padConstant = 0x00000000;

								void WritePaddingY (bool pre) {
									if (!hasPadding.Y)
										return;
									if (pre && !shouldPad.Y)
										return;
									int lastStrideOffset = 0;
									foreach (int i in 0.Until(texture.Padding.Y)) {
										int strideOffset = y * paddedSize.Width;
										lastStrideOffset = strideOffset;
										foreach (int x in 0.Until(paddedSize.Width)) {
											paddedData[strideOffset + x] = padConstant;
										}
										++y;
									}
									if (!pre) {
										foreach (int i in 0.Until(texture.BlockPadding.Y)) {
											int strideOffset = y * paddedSize.Width;
											foreach (int x in 0.Until(paddedSize.Width)) {
												paddedData[strideOffset + x] = paddedData[lastStrideOffset + x]; // For block-padding, we clamp the data to inhibit edge artifacts.
											}
											++y;
										}
									}
								}

								WritePaddingY(true);

								foreach (int i in 0.Until(spriteSize.Height)) {
									int strideOffset = y * paddedSize.Width;
									int strideOffsetRaw = (i + input.Size.Top) * prescaleSize.Width;
									// Write a padded X line
									int xOffset = strideOffset;
									void WritePaddingX (bool pre) {
										if (!hasPadding.X)
											return;
										if (pre && !shouldPad.X)
											return;
										int lastXOffset = 0;
										foreach (int x in 0.Until(texture.Padding.X)) {
											lastXOffset = xOffset;
											paddedData[xOffset++] = padConstant;
										}
										if (!pre) {
											foreach (int x in 0.Until(texture.BlockPadding.X)) {
												paddedData[xOffset++] = paddedData[lastXOffset]; // For block-padding, we clamp the data to inhibit edge artifacts.
											}
										}
									}
									WritePaddingX(true);
									foreach (int x in 0.Until(spriteSize.Width)) {
										paddedData[xOffset++] = rawData[strideOffsetRaw + x + input.Size.Left];
									}
									WritePaddingX(false);
									++y;
								}

								WritePaddingY(false);

								prescaleData = new Span<int>(paddedData);
								prescaleSize = paddedSize;
								scaledDimensions = scaledSize = newSize = prescaleSize * scale;
								outputSize = prescaleSize;
								//scaledDimensions = originalPaddedSize * scale;
							}
						}

						bitmapData = new int[scaledSize.Area];

						// TODO add a stride/mask so it won't write to the block padding area.
						try {
							new xBRZScaler(
								scaleMultiplier: scale,
								sourceData: prescaleData,
								sourceWidth: prescaleSize.Width,
								sourceHeight: prescaleSize.Height,
								sourceTarget: outputSize,
								targetData: bitmapData,
								configuration: scalerConfig
							);
						}
						catch (Exception ex) {
							ex.PrintError();
							throw;
						}
						//ColorSpace.ConvertLinearToSRGB(bitmapData, Texel.Ordering.ARGB);
					}
					else {
						bitmapData = rawData.ToArray();
					}

					if (Config.Debug.Sprite.DumpResample) {
						using (var filtered = Textures.CreateBitmap(bitmapData, scaledDimensions, PixelFormat.Format32bppArgb)) {
							using (var dump = GetDumpBitmap(filtered)) {
								var path = Cache.GetDumpPath($"{input.Reference.SafeName().Replace("\\", ".")}.{hashString}.resample-{WrappedX}-{WrappedY}-{texture.Padding.X}-{texture.Padding.Y}.png");
								File.Delete(path);
								dump.Save(path, System.Drawing.Imaging.ImageFormat.Png);
							}
						}
					}

					if (scaledDimensions != newSize) {
						// This should be incredibly rare - we very rarely need to scale back down.
						using (var filtered = Textures.CreateBitmap(bitmapData, scaledDimensions, PixelFormat.Format32bppArgb)) {
							using (var resized = filtered.Resize(newSize, System.Drawing.Drawing2D.InterpolationMode.Bicubic)) {
								var resizedData = resized.LockBits(new Bounds(resized), ImageLockMode.ReadOnly, filtered.PixelFormat);

								try {
									bitmapData = new int[resized.Width * resized.Height];
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
					}

					// Check for special cases
					bool HasAlpha = true;
					bool IsPunchThroughAlpha = false;
					bool IsMasky = false;
					bool hasR = true;
					bool hasG = true;
					bool hasB = true;
					{
						const int MaxShades = 256;
						var alpha = stackalloc int[MaxShades];
						var blue = stackalloc int[MaxShades];
						var green = stackalloc int[MaxShades];
						var red = stackalloc int[MaxShades];

						int idx = 0;
						foreach (var color in bitmapData) {
							var aValue = color.ExtractByte(24);
							if (aValue == 0) {
								// Clear out all other colors for alpha of zero.
								bitmapData[idx] = 0;
							}
							alpha[aValue]++;
							blue[color.ExtractByte(16)]++;
							green[color.ExtractByte(8)]++;
							red[color.ExtractByte(0)]++;

							++idx;
						}

						hasR = red[0] != bitmapData.Length;
						hasG = green[0] != bitmapData.Length;
						hasB = blue[0] != bitmapData.Length;

						//Debug.WarningLn($"Punch-through Alpha: {bitmapData.Length}");
						IsPunchThroughAlpha = IsMasky = ((alpha[0] + alpha[MaxShades - 1]) == bitmapData.Length);
						HasAlpha = (alpha[MaxShades - 1] != bitmapData.Length);

						if (HasAlpha && !IsPunchThroughAlpha) {
							var alphaDeviation = Statistics.StandardDeviation(alpha, MaxShades, 1, MaxShades - 2);
							IsMasky = alphaDeviation < Config.Resample.BlockHardAlphaDeviationThreshold;
						}

						bool grayscale = true;
						foreach (int i in 0.Until(256)) {
							if (blue[i] != green[i] || green[i] != red[i]) {
								grayscale = false;
								break;
							}
						}
						if (grayscale) {
							//Debug.WarningLn($"Grayscale: {bitmapData.Length}");
						}
					}

					bool useBlockCompression = Config.Resample.UseBlockCompression && BlockCompress.IsBlockMultiple(newSize);

					if (useBlockCompression) {
						BlockCompress.Compress(
							data: ref bitmapData,
							format: ref spriteFormat,
							dimensions: newSize,
							HasAlpha: HasAlpha,
							IsPunchThroughAlpha: IsPunchThroughAlpha,
							IsMasky: IsMasky,
							HasR: hasR,
							HasG: hasG,
							HasB: hasB
						);
					}

					Cache.Save(cachePath, newSize, spriteFormat, wrapped, texture.Padding, texture.BlockPadding, bitmapData);
				}

				TotalAdditionalSize += spriteFormat.SizeBytes(newSize.Area);

				/*
				if (useBlockCompression) {
					var blockData = new int[(newSize.Width * newSize.Height) / sizeof(int)];
					fixed (int* p = bitmapData) {
						// public static void CompressImage (IntPtr rgba, int width, int height, IntPtr blocks, SquishFlags flags);
						fixed (int* outData = blockData) {
							Squish.CompressImage((IntPtr)p, newSize.Width, newSize.Height, (IntPtr)outData, SquishFlags.Dxt5 | SquishFlags.WeightColourByAlpha);
						}
					}

					bitmapData = blockData;
				}
				*/

				ManagedTexture2D CreateTexture(int[] data) {
					var newTexture = new ManagedTexture2D(
						texture: texture,
						reference: input.Reference,
						dimensions: newSize,
						format: spriteFormat
					);
					newTexture.SetDataEx(data);
					return newTexture;
				}

				if (Config.AsyncScaling.Enabled && async) {
					var reference = input.Reference;
					Action asyncCall = () => {
						if (reference.IsDisposed) {
							return;
						}
						ManagedTexture2D newTexture = null;
						try {
							newTexture = CreateTexture(bitmapData);
							texture.Texture = newTexture;
							texture.Finish();
						}
						catch (Exception ex) {
							ex.PrintError();
							if (newTexture != null) {
								newTexture.Dispose();
							}
							texture.Destroy(reference);
						}
					};
					ScaledTexture.AddPendingAction(asyncCall);
					return null;
				}
				else {
					ManagedTexture2D newTexture = null;
					try {
						newTexture = CreateTexture(bitmapData);
						output = newTexture;
					}
					catch (Exception ex) {
						ex.PrintError();
						if (newTexture != null) {
							newTexture.Dispose();
						}
					}
				}
			}
			catch (Exception ex) {
				ex.PrintError();
			}

			//TextureCache.Add(hash, output);
			return output;
		}
	}
}
