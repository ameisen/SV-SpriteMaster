using JetBrains.Annotations;
using LinqFasterer;
using Microsoft.Toolkit.HighPerformance;
using Pastel;
using SpriteMaster.Extensions;
using SpriteMaster.Harmonize;
using System;
using System.Runtime.CompilerServices;
using static SpriteMaster.Runtime;

namespace SpriteMaster.Compressors;

// TODO : Implement a continual training dictionary so each stream doesn't require its own dictionary for in-memory compression.
//[HarmonizeFinalizeCatcher<ZstdNet.Compressor, DllNotFoundException>(critical: false)]
internal static class Zstd {
	[Harmonize(
		"ZstdNet.ExternMethods",
		"SetWinDllDirectory",
		priority: Harmonize.Harmonize.PriorityLevel.First,
		instance: false,
		critical: true
	)]
	public static bool SetWinDllDirectory() {
		return false;
	}

	private sealed class Compressor : IDisposable {
		internal readonly ZstdNet.Compressor Delegator;

		[MethodImpl(MethodImpl.Inline)]
		internal Compressor() : this(Options.CompressionDefault) { }
		[MethodImpl(MethodImpl.Inline)]
		internal Compressor(ZstdNet.CompressionOptions options) => Delegator = new(options);


		[MethodImpl(MethodImpl.Inline)]
		public void Dispose() {
			try {
				Delegator.Dispose();
			}
			catch (DllNotFoundException) {
				// This eliminates an invalid call to a DLL that isn't present.
				GC.SuppressFinalize(Delegator);
			}
		}

		[MethodImpl(MethodImpl.Inline)]
		internal byte[] Wrap(byte[] data) => Delegator.Wrap(data);

		[MethodImpl(MethodImpl.Inline)]
		internal byte[] Wrap(ReadOnlySpan<byte> data) => Delegator.Wrap(data);
	}

	private sealed class Decompressor : IDisposable {
		internal readonly ZstdNet.Decompressor Delegator;

		[MethodImpl(MethodImpl.Inline)]
		internal Decompressor() : this(Options.DecompressionDefault) { }
		[MethodImpl(MethodImpl.Inline)]
		internal Decompressor(ZstdNet.DecompressionOptions options) => Delegator = new(options);

		[MethodImpl(MethodImpl.Inline)]
		public void Dispose() => Delegator.Dispose();

		[MethodImpl(MethodImpl.Inline)]
		internal byte[] Unwrap(byte[] data) => Delegator.Unwrap(data);

		[MethodImpl(MethodImpl.Inline)]
		internal byte[] Unwrap(byte[] data, int size) => Delegator.Unwrap(data, size);
	}

	private static bool? IsSupportedInternal = null;
	internal static bool IsSupported {
		[MethodImpl(MethodImpl.RunOnce)]
		get {
			if (IsSupportedInternal.HasValue) {
				return IsSupportedInternal.Value;
			}

			try {
				var dummyData = new byte[16];
				var compressedData = CompressTest(dummyData);
				var uncompressedData = Decompress(compressedData, dummyData.Length);
				if (!dummyData.SequenceEqualF(uncompressedData)) {
					throw new Exception("Original and Uncompressed Data Mismatch");
				}
				Debug.Info("Zstd Compression is supported".Pastel(DrawingColor.LightGreen));
				IsSupportedInternal = true;
			}
			catch (DllNotFoundException) {
				Debug.Info("Zstd Compression not supported".Pastel(DrawingColor.LightGreen));
				IsSupportedInternal = false;
			}
			catch (Exception ex) {
				Debug.Info($"Zstd Compression not supported: '{ex.GetType().Name} {ex.Message}'".Pastel(DrawingColor.Red));
				IsSupportedInternal = false;
			}

			return IsSupportedInternal.Value;
		}
	}

	private static int GetCompressionLevel(Compression.Level level) {
		return level switch {
			Compression.Level.None => Options.CompressionMinimum.CompressionLevel,
			Compression.Level.Fastest => Options.CompressionMinimum.CompressionLevel,
			Compression.Level.Normal => Options.CompressionDefault.CompressionLevel,
			Compression.Level.Maximum => Options.CompressionMaximum.CompressionLevel,
			_ => ThrowHelper.ThrowArgumentOutOfRangeException<int>(nameof(level), level, null)
		};
	}

	private static class Options {
		internal static readonly ZstdNet.CompressionOptions CompressionMinimum = new(ZstdNet.CompressionOptions.MinCompressionLevel);
		internal static readonly ZstdNet.CompressionOptions CompressionDefault = new(ZstdNet.CompressionOptions.DefaultCompressionLevel);
		internal static readonly ZstdNet.CompressionOptions CompressionMaximum = new(ZstdNet.CompressionOptions.MaxCompressionLevel);
		internal static readonly ZstdNet.DecompressionOptions DecompressionDefault = new(null);
	}

	[Pure, MustUseReturnValue, MethodImpl(MethodImpl.Inline)]
	private static Compressor GetEncoder(Compression.Level level) => level switch {
		Compression.Level.None => new(Options.CompressionMinimum),
		Compression.Level.Fastest => new(Options.CompressionMinimum),
		Compression.Level.Normal => new(Options.CompressionDefault),
		Compression.Level.Maximum => new(Options.CompressionMaximum),
		_ => ThrowHelper.ThrowArgumentOutOfRangeException<Compressor>(nameof(level), level, null)
	};

	[Pure, MustUseReturnValue, MethodImpl(MethodImpl.Inline)]
	private static Decompressor GetDecoder() => new(Options.DecompressionDefault);

	[Pure, MustUseReturnValue, MethodImpl(MethodImpl.RunOnce)]
	private static byte[] CompressTest(byte[] data) {
		using var encoder = GetEncoder(Compression.Level.Normal);
		return encoder.Wrap(data);
	}

	[Pure, MustUseReturnValue, MethodImpl(MethodImpl.Inline)]
	private static byte[] CompressBytes(byte[] data, Compression.Level level = Compression.Level.Normal) {
		using var encoder = GetEncoder(level);
		return encoder.Wrap(data);
	}

	[Pure, MustUseReturnValue, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static unsafe long AlignCount<T>(long size) where T : unmanaged =>
		(long)(((ulong)size + (ulong)(sizeof(T) - 1)) / (ulong)sizeof(T));

	[Pure, MustUseReturnValue, MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static unsafe int AlignCount<T>(int size) where T : unmanaged =>
		(int)(((long)size + (sizeof(T) - 1)) / sizeof(T));


	[Pure, MustUseReturnValue, MethodImpl(MethodImpl.Inline)]
	internal static byte[] Compress(byte[] data, Compression.Level level = Compression.Level.Normal) {
		using var encoder = GetEncoder(level);
		return encoder.Wrap(data);
	}

	[Pure, MustUseReturnValue, MethodImpl(MethodImpl.Inline)]
	private static byte[] CompressBytes(ReadOnlySpan<byte> data, Compression.Level level = Compression.Level.Normal) {
		using var encoder = GetEncoder(level);
		return encoder.Wrap(data);
	}

	[Pure, MustUseReturnValue, MethodImpl(MethodImpl.Inline)]
	internal static unsafe T[] Compress<T>(ReadOnlySpan<byte> data, Compression.Level level = Compression.Level.Normal) where T : unmanaged {
		if (typeof(T) == typeof(byte)) {
			return (T[])(object)CompressBytes(data);
		}

		ulong capacityBytes = ZstdNet.Compressor.GetCompressBoundLong((ulong)data.Length);
		long capacityElements = AlignCount<T>((long)capacityBytes);
		if (capacityElements > int.MaxValue) {
			var resultArray = CompressBytes(data, level);
			T[] copiedResult = GC.AllocateUninitializedArray<T>(AlignCount<T>(resultArray.Length));
			resultArray.AsReadOnlySpan().CopyTo(copiedResult.AsSpan().AsBytes());
			return copiedResult;
		}

		using var encoder = GetEncoder(level);
		T[] result = GC.AllocateUninitializedArray<T>((int)capacityElements);
		int length = AlignCount<T>(encoder.Delegator.Wrap(data, result.AsSpan().AsBytes()));
		if (result.Length != length) {
			Array.Resize(ref result, length);
		}

		return result;
	}

	[Pure, MustUseReturnValue, MethodImpl(MethodImpl.Inline)]
	internal static byte[] Decompress(byte[] data) {
		using var decoder = GetDecoder();
		return decoder.Unwrap(data);
	}

	[Pure, MustUseReturnValue, MethodImpl(MethodImpl.Inline)]
	internal static byte[] Decompress(byte[] data, int size) {
		using var decoder = GetDecoder();
		return decoder.Unwrap(data, size);
	}

	[Pure, MustUseReturnValue, MethodImpl(MethodImpl.Inline)]
	internal static unsafe T[] Decompress<T>(byte[] data, int size) where T : unmanaged {
		if (typeof(T) == typeof(byte)) {
			return (T[])(object)Decompress(data, size);
		}

		using var decoder = GetDecoder();
		int capacityElements = AlignCount<T>(size);

		T[] result = GC.AllocateUninitializedArray<T>(capacityElements);
		int length = AlignCount<T>(decoder.Delegator.Unwrap(data, result.AsSpan().AsBytes()));
		if (result.Length != length) {
			Array.Resize(ref result, length);
		}

		return result;
	}
}
