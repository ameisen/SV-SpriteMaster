using Ionic.Zlib;
using SevenZip.Compression.LZMA;
using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions {
	internal static class Compression {
		internal enum Algorithm {
			None = 0,
			Compress = 1,
			Deflate = 2,
			LZMA = 3,
			Zstd = 4,
			Best = Zstd,
		}

		internal static readonly Algorithm BestAlgorithm = GetPreferredAlgorithm();

		internal static Algorithm GetPreferredAlgorithm(Algorithm fromAlgorithm = Algorithm.Best) {
			for (int i = (int)fromAlgorithm; i > 0; --i) {
				var testAlgorithm = (Algorithm)i;

				switch (testAlgorithm) {
					case Algorithm.Compress:
						if (Compressors.SystemIO.IsSupported) {
							return testAlgorithm;
						}
						break;
					case Algorithm.Deflate:
						if (Compressors.Deflate.IsSupported) {
							return testAlgorithm;
						}
						break;
					case Algorithm.LZMA:
						if (Compressors.LZMA.IsSupported) {
							return testAlgorithm;
						}
						break;
					case Algorithm.Zstd:
						if (Compressors.Zstd.IsSupported) {
							return testAlgorithm;
						}
						break;
					default:
						break;
				}
			}

			return Algorithm.None;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] Compress(this byte[] data, Algorithm algorithm) {
			return algorithm switch {
				Algorithm.None => data,
				Algorithm.Compress => Compressors.SystemIO.Compress(data),
				Algorithm.Deflate => Compressors.Deflate.Compress(data),
				Algorithm.LZMA => Compressors.LZMA.Compress(data),
				Algorithm.Zstd => Compressors.Zstd.Compress(data),
				_ => throw new Exception($"Unknown Compression Algorithm: {algorithm}"),
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] Compress(this byte[] data) {
			return Compress(data, BestAlgorithm);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] Decompress (this byte[] data, int size, Algorithm algorithm) {
			if (size == -1) {
				return Decompress(data, algorithm);
			}

			return algorithm switch {
				Algorithm.None => data,
				Algorithm.Compress => Compressors.SystemIO.Decompress(data, size),
				Algorithm.Deflate => Compressors.Deflate.Decompress(data, size),
				Algorithm.LZMA => Compressors.LZMA.Decompress(data, size),
				Algorithm.Zstd => Compressors.Zstd.Decompress(data, size),
				_ => throw new Exception($"Unknown Compression Algorithm: {algorithm}"),
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] Decompress(this byte[] data, int size) {
			return Decompress(data, size, BestAlgorithm);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] Decompress(this byte[] data, Algorithm algorithm) {
			return algorithm switch {
				Algorithm.None => data,
				Algorithm.Compress => Compressors.SystemIO.Decompress(data),
				Algorithm.Deflate => Compressors.Deflate.Decompress(data),
				Algorithm.LZMA => Compressors.LZMA.Decompress(data),
				Algorithm.Zstd => Compressors.Zstd.Decompress(data),
				_ => throw new Exception($"Unknown Compression Algorithm: {algorithm}"),
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] Decompress(this byte[] data) {
			return Decompress(data, BestAlgorithm);
		}

	}
}
