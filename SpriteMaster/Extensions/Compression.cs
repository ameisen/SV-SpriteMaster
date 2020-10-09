using Ionic.Zlib;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions {
	internal static class Compression {
		internal enum Algorithm {
			None = 0,
			Compress = 1,
			Deflate = 2,
			LZMA = 3
		}

		//private static readonly MethodInfo ZlibBaseCompressBuffer;
		//
		//static Compression() {
		//	ZlibBaseCompressBuffer = typeof(ZlibStream).Assembly.GetType("Ionic.Zlib.ZlibBaseStream").GetMethod("CompressBuffer", BindingFlags.Static | BindingFlags.Public);
		//	if (ZlibBaseCompressBuffer == null) {
		//		throw new NullReferenceException(nameof(ZlibBaseCompressBuffer));
		//	}
		//}

		// https://stackoverflow.com/questions/39191950/how-to-compress-a-byte-array-without-stream-or-system-io

		private static class Compressor {
			internal static class Stream {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				internal static int CompressedLengthEstimate(byte[] data) {
					return data.Length >> 1;
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				internal static int DecompressedLengthEstimate(byte[] data) {
					return data.Length << 1;
				}

				internal static byte[] Compress(byte[] data) {
					using var val = new MemoryStream(CompressedLengthEstimate(data));
					using (var compressor = new System.IO.Compression.DeflateStream(val, System.IO.Compression.CompressionLevel.Optimal)) {
						compressor.Write(data, 0, data.Length);
					}
					return val.ToArray();
				}

				internal static byte[] Decompress(byte[] data) {
					using var dataStream = new MemoryStream(data);
					using var val = new MemoryStream(DecompressedLengthEstimate(data));
					using (var compressor = new System.IO.Compression.DeflateStream(dataStream, System.IO.Compression.CompressionMode.Decompress)) {
						compressor.CopyTo(val);
					}
					return val.ToArray();
				}

				internal static byte[] Decompress(byte[] data, int size) {
					using var dataStream = new MemoryStream(data);
					var output = new byte[size];
					using (var val = new MemoryStream(output)) {
						using var compressor = new System.IO.Compression.DeflateStream(dataStream, System.IO.Compression.CompressionMode.Decompress);
						compressor.CopyTo(val);
					}
					return output;
				}
			}

			internal static class Deflate {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				internal static int CompressedLengthEstimate(byte[] data) {
					return data.Length >> 1;
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				internal static byte[] Compress(byte[] data) {
					using var val = new MemoryStream(CompressedLengthEstimate(data));
					using (var compressor = new DeflateStream(val, CompressionMode.Compress, CompressionLevel.BestCompression)) {
						compressor.Strategy = CompressionStrategy.Filtered;
						compressor.Write(data, 0, data.Length);
					}
					return val.ToArray();
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				internal static byte[] Decompress(byte[] data) {
					return DeflateStream.UncompressBuffer(data);
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				internal static byte[] Decompress(byte[] data, int size) {
					using var dataStream = new MemoryStream(data);
					var output = new byte[size];
					using (var val = new MemoryStream(output)) {
						using var compressor = new DeflateStream(dataStream, CompressionMode.Decompress);
						compressor.CopyTo(val);
					}
					return output;
				}
			}

			// https://stackoverflow.com/a/8605828
			internal static class LZMA {
				private static readonly byte[] Properties;

				static LZMA() {
					var encoder = new SevenZip.Compression.LZMA.Encoder();
					using var propertiesStream = new MemoryStream(5);
					encoder.WriteCoderProperties(propertiesStream);
					propertiesStream.Flush();
					Properties = propertiesStream.ToArray();
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				internal static int CompressedLengthEstimate(byte[] data) {
					return data.Length >> 1;
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				internal static int DecompressedLengthEstimate(byte[] data) {
					return data.Length << 1;
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				internal static byte[] Compress(byte[] data) {
					using var output = new MemoryStream(CompressedLengthEstimate(data));

					var encoder = new SevenZip.Compression.LZMA.Encoder();
					using (var input = new MemoryStream(data)) {
						encoder.Code(input, output, data.Length, -1, null);
					}

					output.Flush();
					return output.ToArray();
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				internal static unsafe byte[] Decompress(byte[] data) {
					using var output = new MemoryStream(DecompressedLengthEstimate(data));

					using (var input = new MemoryStream(data)) {
						var decoder = new SevenZip.Compression.LZMA.Decoder();
						decoder.SetDecoderProperties(Properties);
						decoder.Code(input, output, input.Length, -1, null);
					}

					output.Flush();
					return output.ToArray();
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				internal static byte[] Decompress(byte[] data, int size) {
					var output = new byte[size];
					using var outputStream = new MemoryStream(output);

					using (var input = new MemoryStream(data)) {
						var decoder = new SevenZip.Compression.LZMA.Decoder();
						decoder.SetDecoderProperties(Properties);
						decoder.Code(input, outputStream, input.Length, size, null);
					}

					outputStream.Flush();
					return output;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] Compress(this byte[] data, Algorithm algorithm = Algorithm.LZMA) {
			return algorithm switch {
				Algorithm.None => data,
				Algorithm.Compress => Compressor.Stream.Compress(data),
				Algorithm.Deflate => Compressor.Deflate.Compress(data),
				Algorithm.LZMA => Compressor.LZMA.Compress(data),
				_ => throw new Exception($"Unknown Compression Algorithm: {algorithm}"),
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] Decompress (this byte[] data, int size, Algorithm algorithm = Algorithm.LZMA) {
			if (size == -1) {
				return Decompress(data, algorithm);
			}

			return algorithm switch {
				Algorithm.None => data,
				Algorithm.Compress => Compressor.Stream.Decompress(data, size),
				Algorithm.Deflate => Compressor.Deflate.Decompress(data, size),
				Algorithm.LZMA => Compressor.LZMA.Decompress(data, size),
				_ => throw new Exception($"Unknown Compression Algorithm: {algorithm}"),
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] Decompress(this byte[] data, Algorithm algorithm = Algorithm.LZMA) {
			return algorithm switch {
				Algorithm.None => data,
				Algorithm.Compress => Compressor.Stream.Decompress(data),
				Algorithm.Deflate => Compressor.Deflate.Decompress(data),
				Algorithm.LZMA => Compressor.LZMA.Decompress(data),
				_ => throw new Exception($"Unknown Compression Algorithm: {algorithm}"),
			};
		}
	}
}
