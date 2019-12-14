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
using xBRZNet;

namespace SpriteMaster
{
	internal class Upscaler
	{
		private const bool DisableCache = false;

		static Bitmap CreateBitmap(in int[] source, in Vector2I size, PixelFormat format = PixelFormat.Format32bppArgb)
		{
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
			if (CopyWithPadding && hasPadding)
			{
				var rowElements = newImage.Width;
				var rowSize = newBitmapData.Stride;

				int sourceOffset = 0;
				foreach (int row in 0.Until(newImage.Height))
				{
					Marshal.Copy(source, sourceOffset, newBitmapPointer, rowElements);
					sourceOffset += rowElements;
					newBitmapPointer += rowSize;
				}
			}
			else
			{
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

		enum LinkType : int
		{
			File = 0,
			Directory = 1
		}

		[DllImport("kernel32.dll")]
		static extern bool CreateSymbolicLink(string Link, string Target, LinkType Type);

		private static bool IsSymbolic(string path)
		{
			FileInfo pathInfo = new FileInfo(path);
			return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
		}

		static Upscaler()
		{

			if (!DisableCache)
			{
				// Delete any old caches.
				try
				{
					foreach (var root in new string[] { Config.LocalRoot })
					{
						var directories = Directory.EnumerateDirectories(root);
						foreach (var directory in directories)
						{
							try
							{
								if (!Directory.Exists(directory))
								{
									continue;
								}
								if (IsSymbolic(directory))
								{
									continue;
								}
								var endPath = Path.GetFileName(directory);
								if (endPath != CacheName && endPath != JunctionCacheName)
								{
									// If it doesn't match, it's outdated and should be deleted.
									Directory.Delete(directory, true);
								}
							}
							catch { /* Ignore failures */ }
						}
					}
				}
				catch { /* Ignore failures */ }

				// Create the directory path
				Directory.CreateDirectory(LocalDataPath);

				if (Config.Debug.Sprite.DumpReference || Config.Debug.Sprite.DumpResample)
				{
					Directory.CreateDirectory(Path.Combine(LocalDataPath, "dump"));
				}

				// Mark the directory as compressed because this is very space wasteful and we are currently not performing compression.
				// https://stackoverflow.com/questions/624125/compress-a-folder-using-ntfs-compression-in-net
				try
				{
					var dir = new DirectoryInfo(LocalDataPath);
					if ((dir.Attributes & FileAttributes.Compressed) == 0)
					{
						var objectPath = $"Win32_Directory.Name='{dir.FullName.Replace("\\", @"\\").TrimEnd('\\')}'";
						using (ManagementObject obj = new ManagementObject(objectPath))
						{
							using (obj.InvokeMethod("Compress", null, null))
							{
								// I don't really care about the return value, 
								// if we enabled it great but it can also be done manually
								// if really needed
							}
						}
					}
				}
				catch { /* Ignore failures */ }

				// Set up a symbolic link to aid in debugging.
				try
				{
					CreateSymbolicLink(
						Link: Path.Combine(Config.LocalRoot, JunctionCacheName),
						Target: Path.Combine(Config.LocalRoot, CacheName),
						Type: LinkType.Directory
					);
				}
				catch { /* Ignore failure */ }
			}
		}

		private class MetaData
		{
			internal readonly ulong Hash;

			internal MetaData(ulong hash)
			{
				this.Hash = hash;
			}
		}

		private static ConditionalWeakTable<Texture2D, MetaData> MetaCache = new ConditionalWeakTable<Texture2D, MetaData>();
		//private static Dictionary<ulong, Texture2D> TextureCache = new Dictionary<ulong, Texture2D>();

		internal static ulong GetHash(in TextureWrapper input, bool desprite)
		{
			ulong hash;
			lock (MetaCache)
			{
				if (MetaCache.TryGetValue(input.Reference, out var metaData))
				{
					hash = metaData.Hash;
				}
				else
				{
					hash = input.Hash();
					MetaCache.Add(input.Reference, new MetaData(hash: hash));
				}
			}
			if (desprite)
			{
				hash ^= input.Size.Hash();
			}
			return hash;
		}

		internal static Texture2D Upscale(ScaledTexture texture, ref int scale, in TextureWrapper input, bool desprite, ulong hash, ref Vector2B wrapped)
		{
			wrapped.Set(false);

			var output = input.Reference;

			var inputSize = desprite ? new Vector2I(input.Size.Width, input.Size.Height) : new Vector2I(input.ReferenceSize);

			if (Config.Resample.SmartScale && Config.Resample.Scale)
			{
				foreach (int s in Config.Resample.MaxScale.Until(scale))
				{
					var newDimensions = inputSize * s;
					if (newDimensions.X <= Config.ClampDimension && newDimensions.Y <= Config.ClampDimension)
					{
						scale = s;
						break;
					}
				}
			}

			var scaledSize = inputSize * scale;
			var newSize = scaledSize.Min(Config.ClampDimension);

			if (Config.RestrictSize)
			{
				var scaledTestSize = inputSize * scale;
				if (scaledTestSize.Width > Config.ClampDimension)
				{
					newSize.Width = input.ReferenceSize.Width;
				}
				if (scaledTestSize.Height > Config.ClampDimension)
				{
					newSize.Height = input.ReferenceSize.Height;
				}
			}

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
			try
			{
				if (!DisableCache && File.Exists(localDataPath))
				{
					int retries = 100;
					bool success = false;

					while (!success && --retries != 0)
					{
						if (File.Exists(localDataPath))
						{
							try
							{
								using (var reader = new BinaryReader(new FileStream(localDataPath, FileMode.Open, FileAccess.Read, FileShare.Read)))
								{
									bitmapData = new int[(reader.BaseStream.Length - 2) / sizeof(int)];

									wrapped.X = reader.ReadBoolean();
									wrapped.Y = reader.ReadBoolean();

									foreach (int i in 0.Until(bitmapData.Length))
									{
										bitmapData[i] = reader.ReadInt32();
									}
								}
								success = true;
							}
							catch (IOException)
							{
								Thread.Sleep(16);
							}
						}
					}
				}
				
				if (bitmapData == null)
				{
					Bitmap GetDumpBitmap(Bitmap source)
					{
						var dump = (Bitmap)source.Clone();
						foreach (int y in 0.Until(dump.Height))
							foreach (int x in 0.Until(dump.Width))
							{
								dump.SetPixel(
									x, y,
									Texel.FromARGB(dump.GetPixel(x, y)).Color
								);
							}

						return dump;
					}

					Vector2B Wrapped = new Vector2B(input.Wrapped);

					unsafe
					{
						using (var rawDataHandle = input.Data.AsMemory().Pin())
						{
							var rawData = new Span<int>(rawDataHandle.Pointer, input.Data.Length / sizeof(int));

							if (Config.Resample.DeSprite && Config.WrapDetection.Enabled && Config.Resample.EnableWrappedAddressing)
							{
								byte GetAlpha(int sample)
								{
									return (byte)(((uint)sample >> 24) & 0xFF);
								}

								// Count the fragments that are not alphad out completely on the edges.
								// Both edges must meet the threshold.
								if (!Wrapped.X)
								{
									int[] samples = new int[] { 0, 0 };
									foreach (int y in 0.Until(inputSize.Height))
									{
										int offset = (y + input.Size.Y) * inputSize.Width + input.Size.X;
										int sample0 = rawData[offset];
										int sample1 = rawData[offset + (inputSize.Width - 1)];

										if (GetAlpha(sample0) >= Config.WrapDetection.alphaThreshold)
										{
											samples[0]++;
										}
										if (GetAlpha(sample1) >= Config.WrapDetection.alphaThreshold)
										{
											samples[1]++;
										}
									}
									int threshold = ((float)inputSize.Height * Config.WrapDetection.edgeThreshold).RoundToInt();
									Wrapped.X = samples[0] >= threshold && samples[1] >= threshold;
								}
								if (!Wrapped.Y)
								{
									int[] samples = new int[] { 0, 0 };
									int[] offsets = new int[] { input.Size.Y * inputSize.Width, input.Size.Y + (inputSize.Height - 1) * inputSize.Width };
									int sampler = 0;
									foreach (int yOffset in offsets)
									{
										foreach (int x in 0.Until(inputSize.Width))
										{
											int offset = yOffset + x + input.Size.X;
											int sample = rawData[offset];
											if (GetAlpha(sample) >= Config.WrapDetection.alphaThreshold)
											{
												samples[sampler]++;
											}
										}
										sampler++;
									}
									int threshold = ((float)inputSize.Width * Config.WrapDetection.edgeThreshold).RoundToInt();
									Wrapped.Y = samples[0] >= threshold && samples[1] >= threshold;
								}
							}

							wrapped = Wrapped = (Wrapped & Config.Resample.EnableWrappedAddressing);

							/*
							if (Config.Debug.Sprite.DumpReference)
							{
								using (var filtered = CreateBitmap(rawData.ToArray(), input.ReferenceSize, PixelFormat.Format32bppArgb))
								{
									var dump = GetDumpBitmap(filtered);
									dump.Save(Path.Combine(LocalDataPath, "dump", $"{input.Reference.SafeName().Replace("\\", ".")}.{hashString}.reference.png"), ImageFormat.Png);
								}
							}
							*/

							if (Config.Resample.Smoothing)
							{
								bitmapData = new int[scaledSize.Area];
								var scalerConfig = new ScalerConfiguration() { WrappedX = Wrapped.X, WrappedY = Wrapped.Y };

								new xBRZScaler(
									scaleMultiplier: scale,
									sourceData: rawData,
									sourceWidth: input.ReferenceSize.Width,
									sourceHeight: input.ReferenceSize.Height,
									sourceTarget: input.Size,
									targetData: bitmapData,
									configuration: scalerConfig
								);
							}
							else
							{
								bitmapData = rawData.ToArray();
							}
						}
					}

					/*
					if (Config.Debug.Sprite.DumpResample)
					{
						var dump = GetDumpBitmap(filtered);
						dump.Save(Path.Combine(LocalDataPath, "dump", $"{input.Reference.SafeName().Replace("\\", ".")}.{hashString}.resample-{WrappedX}-{WrappedY}.png"), ImageFormat.Png);
					}
					*/

					var scaledDimensions = new Vector2I(input.Size.Width * scale, input.Size.Height * scale);

					if (scaledDimensions.Width != newSize.Width || scaledDimensions.Height != newSize.Height)
					{
						// This should be incredibly rare - we very rarely need to scale back down.
						using (var filtered = CreateBitmap(bitmapData, scaledDimensions, PixelFormat.Format32bppArgb))
						{
							var resized = filtered.Resize(newSize, System.Drawing.Drawing2D.InterpolationMode.Bicubic);
							var resizedData = resized.LockBits(new Bounds(resized), ImageLockMode.ReadOnly, filtered.PixelFormat);
							bitmapData = new int[resized.Width * resized.Height];

							try
							{
								var dataSize = resizedData.Stride * resizedData.Height;
								var dataPtr = resizedData.Scan0;
								var widthSize = resizedData.Width * sizeof(int);

								var dataBytes = new byte[dataSize];
								int offsetSource = 0;
								int offsetDest = 0;
								foreach (int y in 0.Until(resizedData.Height))
								{
									Marshal.Copy(dataPtr + offsetSource, bitmapData, offsetDest, widthSize);
									offsetSource += resizedData.Stride;
									offsetDest += widthSize;
								}
							}
							finally
							{
								resized.UnlockBits(resizedData);
							}

						}
					}

					if (!DisableCache)
					{
						try
						{
							using (var writer = new BinaryWriter(File.OpenWrite(localDataPath)))
							{
								writer.Write(wrapped.X);
								writer.Write(wrapped.Y);

								foreach (var v in bitmapData)
								{
									writer.Write(v);
								}
							}
						}
						catch { }
					}
				}

				if (Config.AsyncScaling.Enabled)
				{
					var reference = input.Reference;
					Action asyncCall = () =>
					{
						if (reference.IsDisposed)
						{
							return;
						}
						Texture2D newTexture = new Texture2D(reference.GraphicsDevice, newSize.Width, newSize.Height, false, SurfaceFormat.Color);
						try
						{
							newTexture.SetData(bitmapData);
							texture.Texture = newTexture;
							texture.Finish();
						}
						catch
						{
							newTexture.Dispose();
							texture.Destroy(reference);
						}
					};
					ScaledTexture.AddPendingAction(asyncCall);
					return null;
				}
				else
				{
					Texture2D newTexture = new Texture2D(input.Reference.GraphicsDevice, newSize.Width, newSize.Height, false, SurfaceFormat.Color);
					try
					{
						newTexture.SetData(bitmapData);
						output = newTexture;
					}
					catch
					{
						newTexture.Dispose();
					}
				}
			}
			catch (Exception ex)
			{
				Debug.ErrorLn($"An exception was caught during texture processing: {ex.Message}");
			}

			//TextureCache.Add(hash, output);
			return output;
		}
	}
}
