using LinqFasterer;
using Microsoft.Toolkit.HighPerformance;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Colors;
using SpriteMaster.Extensions;
using SpriteMaster.Metadata;
using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;
using Tomlyn.Syntax;

namespace SpriteMaster.Resample;

sealed class Resampler {
	internal enum Scaler : int {
		xBRZ = 0,
		Bilinear,
		Bicubic,
		ImageMagick
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void PurgeHash(Texture2D reference) {
		reference.Meta().CachedRawData = null;
	}

	// https://stackoverflow.com/a/12996028
	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static ulong HashULong(ulong x) {
		if (x == 0) {
			x = ulong.MaxValue;
		}
		x = (x ^ x >> 30) * 0xbf58476d1ce4e5b9ul;
		x = (x ^ x >> 27) * 0x94d049bb133111ebul;
		x ^= x >> 31;
		return x;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong GetHash(SpriteInfo input, TextureType textureType) {
		// Need to make Hashing.CombineHash work better.
		ulong hash = input.Hash;

		if (Config.Resample.EnableDynamicScale) {
			hash = Hashing.Combine(hash, HashULong(input.ExpectedScale));
		}

		if (textureType == TextureType.Sprite) {
			hash = Hashing.Combine(hash, input.Bounds.Extent.GetLongHashCode());
		}
		return hash;
	}

	private static readonly WeakSet<Texture2D> GarbageMarkSet = Config.Garbage.CollectAccountUnownedTextures ? new() : null;

	private sealed class Tracer : IDisposable {
#if REALLY_TRACE
			private readonly string Name;
			private static int Depth = 0;
#endif

#if REALLY_TRACE
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			[Conditional("REALLY_TRACE")]
			private static void Trace (string msg) {
				Debug.TraceLn($"[CreateNewTexture] {new string(' ', Depth)}{msg}");
			}
#endif

		[MethodImpl(Runtime.MethodImpl.Hot)]
		internal Tracer(string name) {
#if REALLY_TRACE
				Name = name;

				Trace(Name);
				++Depth;
#endif
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		public void Dispose() {
#if REALLY_TRACE
				--Depth;
				Trace("~" + Name);
#endif
		}
	}

	private const int WaterBlock = 4;
	private const int FontBlock = 1;

	// TODO : use MemoryFailPoint class. Extensively.

	private enum GammaState {
		Linear,
		Gamma,
		Unknown
	}

	private static unsafe Span<byte> CreateNewTexture(
		ScaledTexture texture,
		bool async,
		SpriteInfo input,
		string hashString,
		ref Vector2B wrapped,
		ref uint scale,
		out Vector2I size,
		out TextureFormat format,
		out Vector2I padding,
		out Vector2I blockPadding
	) {
		padding = Vector2I.Zero;
		blockPadding = Vector2I.Zero;

		if (Config.Debug.Sprite.DumpReference) {
			Textures.DumpTexture(
				source: input.ReferenceData,
				sourceSize: input.ReferenceSize,
				destBounds: input.Bounds,
				path: Cache.GetDumpPath($"{input.Reference.SafeName().Replace("/", ".")}.{hashString}.reference.png")
			);
		}

		var initialGammaState = GammaState.Gamma;
		var currentGammaState = initialGammaState;

		Bounds inputBounds;
		switch (input.TextureType) {
			case TextureType.Sprite:
				inputBounds = input.Bounds;
				break;
			case TextureType.Image:
				inputBounds = input.ReferenceSize;
				break;
			case TextureType.SlicedImage:
				throw new NotImplementedException("Sliced Images not yet implemented");
			default:
				throw new NotImplementedException("Unknown Texture Type provided");
		}

		if (input.Reference.Format.IsCompressed()) {
			throw new InvalidOperationException($"Compressed texture '{input.Reference.SafeName()}' reached Resampler");
		}

		// Water in the game is pre-upscaled by 4... which is weird.
		Span<uint> spriteRawData;
		Bounds spriteRawBounds;
		int blockSize = 1;
		if ((input.IsWater || input.Reference == StardewValley.Game1.rainTexture) && WaterBlock != 1) {
			blockSize = WaterBlock;
		}
		else if (input.IsFont && FontBlock != 1) {
			blockSize = FontBlock;
			scale = Config.Resample.MaxScale;
		}
		// TODO : handle inverted input.Bounds
		spriteRawData = Passes.ExtractSprite.Extract(
			data: input.ReferenceData.AsSpan<byte>().Cast<byte, Color8>(),
			textureBounds: input.Reference.Bounds,
			spriteBounds: inputBounds,
			stride: input.ReferenceSize.Width,
			block: blockSize,
			newBounds: out spriteRawBounds
		).Cast<Color8, uint>();

		// At this point, rawData includes just the sprite's raw data.

		if (Config.Resample.Scale) {
			var originalScale = scale;
			scale = 2;
			foreach (uint s in originalScale.RangeTo(2U)) {
				var newDimensions = spriteRawBounds.Extent * s;
				if (newDimensions.X <= Config.PreferredMaxTextureDimension && newDimensions.Y <= Config.PreferredMaxTextureDimension) {
					scale = s;
					break;
				}
			}
		}

		var scaledSize = spriteRawBounds.Extent * scale;
		var newSize = scaledSize.Min(Config.ClampDimension);

		var scaledDimensions = spriteRawBounds.Extent * scale;

		Debug.Info($"Current Draw State: {DrawState.CurrentBlendState}");

		wrapped.Set(false);

		Vector2B Wrapped;
		Vector2B RepeatX, RepeatY;

		var edgeResults = Edge.AnalyzeLegacy(
			reference: input.Reference,
			data: spriteRawData,
			bounds: spriteRawBounds,
			Wrapped: input.Wrapped
		);

		Wrapped = edgeResults.Wrapped;
		RepeatX = edgeResults.RepeatX;
		RepeatY = edgeResults.RepeatY;
		if (Config.Resample.EnableWrappedAddressing) {
			wrapped = Wrapped;
		}

		Span<byte> bitmapData;

		if (Config.Resample.Enabled) {
			var prescaleData = spriteRawData;
			var prescaleSize = spriteRawBounds.Extent;

			var outputSize = spriteRawBounds;

			// Do we need to pad the sprite?
			if (Config.Resample.Padding.Enabled) {
				var shouldPad = new Vector2B(
					!Wrapped.X && !RepeatX.Any && spriteRawBounds.Width > 1,
					!Wrapped.Y && !RepeatY.Any && spriteRawBounds.Height > 1
				);

				if (
					(
						prescaleSize.X <= Config.Resample.Padding.MinimumSizeTexels &&
						prescaleSize.Y <= Config.Resample.Padding.MinimumSizeTexels
					) ||
					Config.Resample.Padding.IgnoreUnknown && !input.Reference.Anonymous()
				) {
					shouldPad = Vector2B.False;
				}

				// TODO : make X and Y variants of the whitelist and blacklist
				if (!input.Reference.Anonymous()) {
					if (Config.Resample.Padding.Whitelist.Contains(input.Reference.SafeName())) {
						shouldPad = Vector2B.True;
					}
					else if (input.IsWater || Config.Resample.Padding.Blacklist.Contains(input.Reference.SafeName())) {
						shouldPad = Vector2B.False;
					}
				}

				if (shouldPad.Any) {
					var expectedPadding = Math.Max(1U, scale / 2);
					var expectedPaddingBoth = expectedPadding * 2;

					// TODO we only need to pad the edge that has texels. Double padding is wasteful.
					var paddedSize = spriteRawBounds.Extent;
					var spriteSize = spriteRawBounds.Extent;

					var actualPadding = Vector2I.Zero;

					if (shouldPad.X) {
						if ((paddedSize.X + expectedPaddingBoth) * scale > Config.ClampDimension) {
							shouldPad.X = false;
						}
						else {
							paddedSize.X += (int)expectedPaddingBoth;
							actualPadding.X = (int)expectedPadding;
						}
					}
					if (shouldPad.Y) {
						if ((paddedSize.Y + expectedPaddingBoth) * scale > Config.ClampDimension) {
							shouldPad.Y = false;
						}
						else {
							paddedSize.Y += (int)expectedPaddingBoth;
							actualPadding.Y = (int)expectedPadding;
						}
					}

					var hasPadding = shouldPad;

					if (hasPadding.Any) {
						uint[] paddedData = GC.AllocateUninitializedArray<uint>(paddedSize.Area);

						int y = 0;

						const int padConstant = 0x00000000;

						padding = actualPadding * scale * 2;

						void WritePaddingY() {
							if (!hasPadding.Y)
								return;
							foreach (int i in 0.RangeTo(actualPadding.Y)) {
								var strideOffset = y * paddedSize.Width;
								foreach (int x in 0.RangeTo(paddedSize.Width)) {
									paddedData[strideOffset + x] = padConstant;
								}
								++y;
							}
						}

						WritePaddingY();

						foreach (int i in 0.RangeTo(spriteSize.Height)) {
							var strideOffset = y * paddedSize.Width;
							var strideOffsetRaw = i * prescaleSize.Width;
							// Write a padded X line
							var xOffset = strideOffset;
							void WritePaddingX() {
								if (!hasPadding.X)
									return;
								foreach (int x in 0.RangeTo(actualPadding.X)) {
									paddedData[xOffset++] = padConstant;
								}
							}
							WritePaddingX();
							foreach (int x in 0.RangeTo(spriteSize.Width)) {
								paddedData[xOffset++] = spriteRawData[strideOffsetRaw + x];
							}
							WritePaddingX();
							++y;
						}

						WritePaddingY();

						prescaleData = paddedData;
						prescaleSize = paddedSize;
						scaledDimensions = scaledSize = newSize = prescaleSize * scale;
						outputSize = prescaleSize;
						//scaledDimensions = originalPaddedSize * scale;
					}
				}
			}

			bitmapData = SpanExt.MakePinned<byte>(scaledSize.Area * sizeof(uint));

			static byte ByteMul(byte a, byte b) => (byte)(a * b + 255 >> 8);
			static byte ByteDiv(byte numerator, byte denominator) {
				if (denominator == 0) {
					return numerator; // this isn't right but I have no idea what to do in this case.
				}
				return (byte)Math.Min(255, numerator / denominator << 8);
			}

			try {
				var doWrap = wrapped | input.IsWater;

				if (Config.Resample.AssumeGammaCorrected && currentGammaState == GammaState.Gamma) {
					for (int y = 0; y < outputSize.Height; ++y) {
						int yInStride = (y + outputSize.Y) * prescaleSize.Width;
						for (int x = 0; x < outputSize.Width; ++x) {
							var sample = (uint)prescaleData[yInStride + x + outputSize.X];

							byte r = (byte)(sample & 0xFF);
							byte g = (byte)(sample >> 8 & 0xFF);
							byte b = (byte)(sample >> 16 & 0xFF);

							r = ColorSpace.sRGB_Precise.Linearize(r);
							g = ColorSpace.sRGB_Precise.Linearize(g);
							b = ColorSpace.sRGB_Precise.Linearize(b);

							sample = r | (uint)(g << 8) | (uint)(b << 16) | sample & 0xFF_00_00_00;

							prescaleData[yInStride + x + outputSize.X] = sample;
						}
						currentGammaState = GammaState.Linear;
					}
				}

				if (!input.IsWater && (Config.Resample.PremultiplyAlpha && edgeResults.PremultipliedAlpha)) {
					for (int y = 0; y < scaledSize.Height; ++y) {
						int yInStride = y * scaledSize.Width * 4;
						for (int x = 0; x < scaledSize.Width; ++x) {
							int actualX = x * 4;

							ref byte r = ref bitmapData[yInStride + actualX + 0];
							ref byte g = ref bitmapData[yInStride + actualX + 1];
							ref byte b = ref bitmapData[yInStride + actualX + 2];
							ref byte a = ref bitmapData[yInStride + actualX + 3];

							r = ByteDiv(r, a);
							g = ByteDiv(g, a);
							b = ByteDiv(b, a);
						}
					}
				}

				if (Config.Resample.Deposterization.Enabled) {
					if (outputSize.X != 0 || outputSize.Y != 0 || outputSize.Extent != prescaleSize) {
						int subArea = outputSize.Area;
						var subData = GC.AllocateUninitializedArray<uint>(subArea);
						for (int y = 0; y < outputSize.Height; ++y) {
							int yInStride = (y + outputSize.Y) * prescaleSize.Width;
							int yOutStride = y * outputSize.Width;
							for (int x = 0; x < outputSize.Width; ++x) {
								subData[yOutStride + x] = prescaleData[yInStride + x + outputSize.X];
							}
						}

						prescaleData = subData;
						prescaleSize = outputSize.Extent;
						outputSize = new Bounds(outputSize.Extent);
					}

					prescaleData = Deposterize.Enhance<uint>(prescaleData, prescaleSize, doWrap);

					if (Config.Debug.Sprite.DumpReference) {
						Textures.DumpTexture(
							source: prescaleData,
							sourceSize: prescaleSize,
							adjustGamma: 2.2,
							path: Cache.GetDumpPath($"{input.Reference.SafeName().Replace("/", ".")}.{hashString}.reference.deposter.png")
						);
					}
				}

				switch (Config.Resample.Scaler) {
					case Scaler.xBRZ: {
							var scalerConfig = new xBRZ.Config(
								Wrapped: doWrap,
								Gamma: false,
								luminanceWeight: Config.Resample.xBRZ.LuminanceWeight,
								equalColorTolerance: Config.Resample.xBRZ.EqualColorTolerance,
								dominantDirectionThreshold: Config.Resample.xBRZ.DominantDirectionThreshold,
								steepDirectionThreshold: Config.Resample.xBRZ.SteepDirectionThreshold,
								centerDirectionBias: Config.Resample.xBRZ.CenterDirectionBias
							);

							new xBRZ.Scaler(
								scaleMultiplier: scale,
								sourceData: prescaleData, // TODO
								sourceSize: prescaleSize,
								sourceTarget: outputSize,
								targetData: bitmapData.Cast<byte, uint>(),
								configuration: scalerConfig
							);
						}
						break;
					case Scaler.ImageMagick: {
							throw new NotImplementedException("ImageMagick Scaling is not implemented");
						}
						break;
					case Scaler.Bilinear:
					case Scaler.Bicubic: {
							throw new NotImplementedException("Bilinear and Bicubic scaling are not implemented");
						}
						break;
					default:
						throw new InvalidOperationException($"Unknown Scaler Type: {Config.Resample.Scaler}");
				}

				if (Config.Resample.Deposterization.Enabled) {
					bitmapData = Deposterize.Enhance<byte>(bitmapData, scaledSize, doWrap);
				}

				if (Config.Resample.UseColorEnhancement) {
					bitmapData = Recolor.Enhance<byte>(bitmapData, scaledSize);
				}

				if (!input.IsWater && (Config.Resample.PremultiplyAlpha && edgeResults.PremultipliedAlpha)) {
					for (int y = 0; y < scaledSize.Height; ++y) {
						int yInStride = y * scaledSize.Width * 4;
						for (int x = 0; x < scaledSize.Width; ++x) {
							int actualX = x * 4;

							ref byte r = ref bitmapData[yInStride + actualX + 0];
							ref byte g = ref bitmapData[yInStride + actualX + 1];
							ref byte b = ref bitmapData[yInStride + actualX + 2];
							ref byte a = ref bitmapData[yInStride + actualX + 3];

							r = ByteMul(r, a);
							g = ByteMul(g, a);
							b = ByteMul(b, a);
						}
					}
				}

				if (Config.Resample.AssumeGammaCorrected && currentGammaState == GammaState.Linear) {
					for (int y = 0; y < scaledSize.Height; ++y) {
						int yInStride = y * scaledSize.Width * 4;
						for (int x = 0; x < scaledSize.Width; ++x) {
							int actualX = x * 4;

							ref byte r = ref bitmapData[yInStride + actualX + 0];
							ref byte g = ref bitmapData[yInStride + actualX + 1];
							ref byte b = ref bitmapData[yInStride + actualX + 2];

							r = ColorSpace.sRGB_Precise.Delinearize(r);
							g = ColorSpace.sRGB_Precise.Delinearize(g);
							b = ColorSpace.sRGB_Precise.Delinearize(b);
						}
					}
					currentGammaState = GammaState.Gamma;
				}
			}
			catch (Exception ex) {
				ex.PrintError();
				throw;
			}
			//ColorSpace.ConvertLinearToSRGB(bitmapData, Texel.Ordering.ARGB);
		}
		else {
			bitmapData = spriteRawData.Cast<uint, byte>();
		}

		if (Config.Debug.Sprite.DumpResample) {
			static string SimplifyBools(in Vector2B vec) {
				return $"{(vec.X ? 1 : 0)}{(vec.Y ? 1 : 0)}";
			}

			Textures.DumpTexture(
				source: bitmapData,
				sourceSize: scaledDimensions,
				swap: (2, 1, 0, 4),
				path: Cache.GetDumpPath($"{input.Reference.SafeName().Replace("/", ".")}.{hashString}.resample-wrap[{SimplifyBools(Wrapped)}]-repeat[{SimplifyBools(RepeatX)},{SimplifyBools(RepeatY)}]-pad[{padding.X},{padding.Y}].png")
			);
		}

		if (scaledDimensions != newSize) {
			if (scaledDimensions.Width < newSize.Width || scaledDimensions.Height < newSize.Height) {
				throw new Exception($"Resampled texture size {scaledDimensions} is smaller than expected {newSize}");
			}

			Debug.TraceLn($"Sprite {texture.SafeName()} requires rescaling");
			// This should be incredibly rare - we very rarely need to scale back down.
			// I don't actually have a solution for this case.
			newSize = scaledDimensions;
		}

		format = TextureFormat.Color;

		if (currentGammaState != initialGammaState) {
			throw new Exception("Gamma State Mismatch");
		}

		// We don't want to use block compression if asynchronous loads are enabled but this is not an asynchronous load... unless that is explicitly enabled.
		if (Config.Resample.BlockCompression.Enabled /*&& (Config.Resample.BlockCompression.Synchronized || !Config.AsyncScaling.Enabled || async)*/ && newSize.MinOf >= 4) {
			// TODO : We can technically allocate the block padding before the scaling phase, and pass it a stride
			// so it will just ignore the padding areas. That would be more efficient than this.

			// Check for special cases
			bool HasAlpha = true;
			bool IsPunchThroughAlpha = false;
			bool IsMasky = false;
			bool hasR = true;
			bool hasG = true;
			bool hasB = true;
			{
				const int MaxShades = 256;

				Span<int> alpha = stackalloc int[MaxShades];
				Span<int> blue = stackalloc int[MaxShades];
				Span<int> green = stackalloc int[MaxShades];
				Span<int> red = stackalloc int[MaxShades];
				for (int i = 0; i < MaxShades; ++i) {
					alpha[i] = 0;
					blue[i] = 0;
					green[i] = 0;
					red[i] = 0;
				}

				var intData = bitmapData.Cast<byte, uint>();

				foreach (var color in intData) {
					alpha[color.ExtractByte(24)]++;
					blue[color.ExtractByte(16)]++;
					green[color.ExtractByte(8)]++;
					red[color.ExtractByte(0)]++;
				}


				hasR = red[0] != intData.Length;
				hasG = green[0] != intData.Length;
				hasB = blue[0] != intData.Length;

				//Debug.WarningLn($"Punch-through Alpha: {intData.Length}");
				IsPunchThroughAlpha = IsMasky = alpha[0] + alpha[MaxShades - 1] == intData.Length;
				HasAlpha = alpha[MaxShades - 1] != intData.Length;

				if (HasAlpha && !IsPunchThroughAlpha) {
					var alphaDeviation = Statistics.StandardDeviation(alpha, MaxShades, 1, MaxShades - 2);
					IsMasky = alphaDeviation < Config.Resample.BlockCompression.HardAlphaDeviationThreshold;
				}
			}

			if (!Decoder.BlockDecoderCommon.IsBlockMultiple(newSize)) {
				var blockPaddedSize = newSize + 3 & ~3;

				var newBuffer = SpanExt.MakeUninitialized<byte>(blockPaddedSize.Area * sizeof(uint));
				var intSpanSrc = bitmapData.Cast<byte, uint>();
				var intSpanDst = newBuffer.Cast<byte, uint>();

				int y;
				for (y = 0; y < newSize.Y; ++y) {
					var newBufferOffset = y * blockPaddedSize.X;
					var bitmapOffset = y * newSize.X;
					int x;
					for (x = 0; x < newSize.X; ++x) {
						intSpanDst[newBufferOffset + x] = intSpanSrc[bitmapOffset + x];
					}
					int lastX = x - 1;
					for (; x < blockPaddedSize.X; ++x) {
						intSpanDst[newBufferOffset + x] = intSpanSrc[bitmapOffset + lastX];
					}
				}
				var lastY = y - 1;
				var sourceOffset = lastY * newSize.X;
				for (; y < blockPaddedSize.Y; ++y) {
					int newBufferOffset = y * blockPaddedSize.X;
					for (int x = 0; x < blockPaddedSize.X; ++x) {
						intSpanDst[newBufferOffset + x] = intSpanDst[sourceOffset + x];
					}
				}

				bitmapData = newBuffer;
				blockPadding += blockPaddedSize - newSize;
				newSize = blockPaddedSize;
			}

			bitmapData = TextureEncode.Encode(
				data: bitmapData,
				format: ref format,
				dimensions: newSize,
				hasAlpha: HasAlpha,
				isPunchthroughAlpha: IsPunchThroughAlpha,
				isMasky: IsMasky,
				hasR: hasR,
				hasG: hasG,
				hasB: hasB
			);
		}

		size = newSize;
		return bitmapData;
	}

	internal static ManagedTexture2D Upscale(ScaledTexture texture, ref uint scale, SpriteInfo input, ulong hash, ref Vector2B wrapped, bool async) {
		try {
			// Try to process the texture twice. Garbage collect after a failure, maybe it'll work then.
			foreach (var _ in 0.To(1)) {
				try {
					return UpscaleInternal(
						texture: texture,
						scale: ref scale,
						input: input,
						hash: hash,
						wrapped: ref wrapped,
						async: async
					);
				}
				catch (OutOfMemoryException) {
					Debug.WarningLn("OutOfMemoryException encountered during Upscale, garbage collecting and deferring.");
					Garbage.Collect(compact: true, blocking: true, background: false);
				}
			}
		}
		catch (Exception ex) {
			Debug.Error($"Internal Error processing '{input}'", ex);
		}

		texture.Texture = null;
		return null;
	}

	internal static readonly Action<Texture2D, int, byte[], int, int> PlatformSetData = typeof(Texture2D).GetMethods(
		System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
		).SingleF(m => m.Name == "PlatformSetData" && m.GetParameters().Length == 4)?.MakeGenericMethod(new Type[] { typeof(byte) })?.CreateDelegate<Action<Texture2D, int, byte[], int, int>>();

	private static ManagedTexture2D UpscaleInternal(ScaledTexture texture, ref uint scale, SpriteInfo input, ulong hash, ref Vector2B wrapped, bool async) {
		var spriteFormat = TextureFormat.Color;

		if (Config.Garbage.CollectAccountUnownedTextures && GarbageMarkSet.Add(input.Reference)) {
			Garbage.Mark(input.Reference);
			input.Reference.Disposing += (obj, _) => {
				Garbage.Unmark((Texture2D)obj);
			};
		}

		var hashString = hash.ToString("x");
		var cachePath = Cache.GetPath($"{hashString}.cache");

		var inputSize = input.TextureType switch {
			TextureType.Sprite => input.Bounds.Extent,
			TextureType.Image => input.ReferenceSize,
			TextureType.SlicedImage => throw new NotImplementedException("Sliced Images not yet implemented"),
			_ => throw new NotImplementedException("Unknown Image Type provided")
		};

		Span<byte> bitmapData;
		try {
			var newSize = Vector2I.Zero;

			try {
				if (Cache.Fetch(
					path: cachePath,
					refScale: out var fetchScale,
					size: out newSize,
					format: out spriteFormat,
					wrapped: out wrapped,
					padding: out texture.Padding,
					blockPadding: out texture.BlockPadding,
					data: out bitmapData
				)) {
					scale = fetchScale;
				}
				else {
					bitmapData = null;
				}
			}
			catch (Exception ex) {
				ex.PrintWarning();
				bitmapData = null;
			}

			if (bitmapData == null) {
				try {
					bitmapData = CreateNewTexture(
						async: async,
						texture: texture,
						input: input,
						hashString: hashString,
						wrapped: ref wrapped,
						scale: ref scale,
						size: out newSize,
						format: out spriteFormat,
						padding: out texture.Padding,
						blockPadding: out texture.BlockPadding
					);
				}
				catch (OutOfMemoryException) {
					Debug.Error($"OutOfMemoryException thrown trying to create texture [texture: {texture.SafeName()}, bounds: {input.Bounds}, textureSize: {input.ReferenceSize}, scale: {scale}]");
					throw;
				}

				try {
					Cache.Save(cachePath, scale, newSize, spriteFormat, wrapped, texture.Padding, texture.BlockPadding, bitmapData);
				}
				catch { }
			}

			texture.UnpaddedSize = newSize - (texture.Padding + texture.BlockPadding);
			texture.AdjustedScale = (Vector2)texture.UnpaddedSize / inputSize;

			ManagedTexture2D CreateTexture(byte[] data) {
				if (input.Reference.GraphicsDevice.IsDisposed) {
					return null;
				}
				var newTexture = new ManagedTexture2D(
					texture: texture,
					reference: input.Reference,
					dimensions: newSize,
					format: spriteFormat
				);
				if (PlatformSetData is not null) {
					PlatformSetData(newTexture, 0, data, 0, data.Length);
				}
				else {
					newTexture.SetData(data);
				}

				return newTexture;
			}

			var isAsync = Config.AsyncScaling.Enabled && async;
			if (!isAsync || Config.AsyncScaling.ForceSynchronousStores) {
				var reference = input.Reference;
				var bitmapDataArray = bitmapData.ToArray();
				void syncCall() {
					if (reference.IsDisposed) {
						return;
					}
					if (texture.IsDisposed) {
						return;
					}
					ManagedTexture2D newTexture = null;
					try {
						newTexture = CreateTexture(bitmapDataArray);
						texture.Texture = newTexture;
						texture.Finish();
					}
					catch (Exception ex) {
						ex.PrintError();
						if (newTexture != null) {
							newTexture.Dispose();
						}
						texture.Dispose();
					}
				}
				SynchronizedTasks.AddPendingLoad(syncCall, bitmapData.Length);
				return null;
			}
			else {
				ManagedTexture2D newTexture = null;
				try {
					newTexture = CreateTexture(bitmapData.ToArray());
					if (isAsync) {
						texture.Texture = newTexture;
						texture.Finish();
					}
					return newTexture;
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
		return null;
	}
}
