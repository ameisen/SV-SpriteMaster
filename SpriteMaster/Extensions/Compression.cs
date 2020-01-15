using Ionic.Zlib;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions {
	internal static class Compression {
		internal enum Algorithm {
			None = 0,
			Compress = 1,
			LZ = 2,
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] StreamCompress (byte[] data) {
			using (var val = new MemoryStream()) {
				using (var compressor = new System.IO.Compression.DeflateStream(val, System.IO.Compression.CompressionLevel.Optimal)) {
					compressor.Write(data, 0, data.Length);
				}
				return val.ToArray();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] StreamDecompress (byte[] data) {
			using (var dataStream = new MemoryStream(data)) {
				using (var val = new MemoryStream()) {
					using (var compressor = new System.IO.Compression.DeflateStream(dataStream, System.IO.Compression.CompressionMode.Decompress)) {
						compressor.CopyTo(val);
					}
					return val.ToArray();
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] StreamDecompress (byte[] data, int size) {
			using (var dataStream = new MemoryStream(data)) {
				var output = new byte[size];
				using (var val = new MemoryStream(output)) {
					using (var compressor = new System.IO.Compression.DeflateStream(dataStream, System.IO.Compression.CompressionMode.Decompress)) {
						compressor.CopyTo(val);
					}
				}
				return output;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] LZCompress (byte[] data) {
			using (var val = new MemoryStream()) {
				using (var compressor = new DeflateStream(val, CompressionMode.Compress, CompressionLevel.BestCompression)) {
					compressor.Write(data, 0, data.Length);
				}
				return val.ToArray();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] LZDecompress (byte[] data) {
			return DeflateStream.UncompressBuffer(data);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static byte[] LZDecompress (byte[] data, int size) {
			using (var dataStream = new MemoryStream(data)) {
				var output = new byte[size];
				using (var val = new MemoryStream(output)) {
					using (var compressor = new DeflateStream(dataStream, CompressionMode.Decompress)) {
						compressor.CopyTo(val);
					}
				}
				return output;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] Compress(this byte[] data, Algorithm algorithm = Algorithm.LZ) {
			switch (algorithm) {
				case Algorithm.None:
					return data;
				case Algorithm.Compress:
					return StreamCompress(data);
				case Algorithm.LZ:
					return LZCompress(data);
				case Algorithm.LZMA:
					throw new NotImplementedException("LZMA support not yet implemented.");
			}
			throw new Exception($"Unknown Compression Algorithm: {algorithm}");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] Decompress (this byte[] data, int size, Algorithm algorithm = Algorithm.LZ) {
			switch (algorithm) {
				case Algorithm.None:
					return data;
				case Algorithm.Compress:
					return StreamDecompress(data, size);
				case Algorithm.LZ:
					return LZDecompress(data, size);
				case Algorithm.LZMA:
					throw new NotImplementedException("LZMA support not yet implemented.");
			}
			throw new Exception($"Unknown Compression Algorithm: {algorithm}");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] Decompress(this byte[] data, Algorithm algorithm = Algorithm.LZ) {
			switch (algorithm) {
				case Algorithm.None:
					return data;
				case Algorithm.Compress:
					return StreamDecompress(data);
				case Algorithm.LZ:
					return LZDecompress(data);
				case Algorithm.LZMA:
					throw new NotImplementedException("LZMA support not yet implemented.");
			}
			throw new Exception($"Unknown Compression Algorithm: {algorithm}");
		}
	}
}
