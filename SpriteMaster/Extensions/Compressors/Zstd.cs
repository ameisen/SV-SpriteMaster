using LinqFasterer;
using Microsoft.Xna.Framework.Graphics;
using Pastel;
using SpriteMaster.Harmonize;
using System;
using System.Runtime.CompilerServices;
using static SpriteMaster.Harmonize.Harmonize;
using static SpriteMaster.Runtime;

namespace SpriteMaster.Extensions.Compressors;
// TODO : Implement a continual training dictionary so each stream doesn't require its own dictionary for in-memory compression.
//[HarmonizeFinalizeCatcher<ZstdNet.Compressor, DllNotFoundException>(critical: false)]
static class Zstd {
	private sealed class Compressor : IDisposable {
		private readonly ZstdNet.Compressor Delegator;

		[MethodImpl(MethodImpl.Hot)]
		internal Compressor() : this(Zstd.Options.CompressionDefault) { }
		[MethodImpl(MethodImpl.Hot)]
		internal Compressor(ZstdNet.CompressionOptions options) => Delegator = new(options);


		[MethodImpl(MethodImpl.Hot)]
		public void Dispose() {
			try {
				Delegator.Dispose();
			}
			catch (DllNotFoundException) {
				// This eliminates an invalid call to a DLL that isn't present.
				GC.SuppressFinalize(Delegator);
			}
		}

		[MethodImpl(MethodImpl.Hot)]
		internal byte[] Wrap(byte[] data) => Delegator.Wrap(data);

		[MethodImpl(MethodImpl.Hot)]
		internal byte[] Wrap(ReadOnlySpan<byte> data) => Delegator.Wrap(data);
	}

	private sealed class Decompressor : IDisposable {
		private readonly ZstdNet.Decompressor Delegate;

		[MethodImpl(MethodImpl.Hot)]
		internal Decompressor() : this(Zstd.Options.DecompressionDefault) { }
		[MethodImpl(MethodImpl.Hot)]
		internal Decompressor(ZstdNet.DecompressionOptions options) => Delegate = new(options);

		[MethodImpl(MethodImpl.Hot)]
		public void Dispose() => Delegate.Dispose();

		[MethodImpl(MethodImpl.Hot)]
		internal byte[] Unwrap(byte[] data) => Delegate.Unwrap(data);

		[MethodImpl(MethodImpl.Hot)]
		internal byte[] Unwrap(byte[] data, int size) => Delegate.Unwrap(data, size);
	}

	internal static bool IsSupported {
		[MethodImpl(Runtime.MethodImpl.RunOnce)]
		get {
			try {
				var dummyData = new byte[16];
				var compressedData = CompressTest(dummyData);
				var uncompressedData = Decompress(compressedData, dummyData.Length);
				if (!EnumerableF.SequenceEqualF(dummyData, uncompressedData)) {
					throw new Exception("Original and Uncompressed Data Mismatch");
				}
				if (Config.Debug.MacOSTestMode) {
					throw new Exception("Mac OS Test Mode Enabled, Zstd not supported");
				}
				Debug.InfoLn("Zstd Compression is supported".Pastel(DrawingColor.LightGreen));
				return true;
			}
			catch (DllNotFoundException) {
				Debug.InfoLn($"Zstd Compression not supported".Pastel(DrawingColor.LightGreen));
				return false;
			}
			catch (Exception ex) {
				Debug.InfoLn($"Zstd Compression not supported: '{ex}'".Pastel(DrawingColor.Red));
				return false;
			}
		}
	}

	private static class Options {
		internal static readonly ZstdNet.CompressionOptions CompressionDefault = new(ZstdNet.CompressionOptions.DefaultCompressionLevel);
		internal static readonly ZstdNet.DecompressionOptions DecompressionDefault = new(null);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static Compressor GetEncoder() => new(Options.CompressionDefault);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static Decompressor GetDecoder() => new(Options.DecompressionDefault);

	[MethodImpl(Runtime.MethodImpl.RunOnce)]
	private static byte[] CompressTest(byte[] data) {
		Compressor encoder = null;
		try {
			using (encoder = GetEncoder()) {
				return encoder.Wrap(data);
			}
		}
		catch (DllNotFoundException) when (encoder is not null) {
			GC.SuppressFinalize(encoder);
			throw;
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static byte[] Compress(byte[] data) {
		using var encoder = GetEncoder();
		return encoder.Wrap(data);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static byte[] Compress(ReadOnlySpan<byte> data) {
		using var encoder = GetEncoder();
		return encoder.Wrap(data);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static byte[] Decompress(byte[] data) {
		using var decoder = GetDecoder();
		return decoder.Unwrap(data);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static byte[] Decompress(byte[] data, int size) {
		using var decoder = GetDecoder();
		return decoder.Unwrap(data, size);
	}
}
