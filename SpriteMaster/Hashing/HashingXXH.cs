using SpriteMaster.Types;
using System;
using System.Buffers;
using System.Data.HashFunction.xxHash;
using System.IO;
using System.Runtime.CompilerServices;

namespace SpriteMaster;

static partial class Hashing {
	private static readonly IxxHash HasherXX = xxHashFactory.Instance.Create(new xxHashConfig() { HashSizeInBits = 64 });

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static ulong HashXXCompute(this byte[] hashData) => BitConverter.ToUInt64(hashData, 0);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong HashXX(this byte[] data) => HasherXX.ComputeHash(data).Hash.HashXXCompute();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong HashXX(this ReadOnlySequence<byte> data) {
		// HasherXX.ComputeHash(new SequenceReader<T>(data)).Hash.HashXXCompute();
		ulong currentHash = Default;
		foreach (var seq in data) {
			currentHash = Combine(currentHash, seq.Span.HashXX());
		}
		return currentHash;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static unsafe ulong HashXX(this in FixedSpan<byte> data) {
		using var stream = new UnmanagedMemoryStream(data.TypedPointer, data.Length);
		return HasherXX.ComputeHash(stream).Hash.HashXXCompute();
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static unsafe ulong HashXX(this ReadOnlySpan<byte> data) {
		fixed (byte* ptr = data) {
			using var stream = new UnmanagedMemoryStream(ptr, data.Length);
			return HasherXX.ComputeHash(stream).Hash.HashXXCompute();
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static unsafe ulong HashXX(byte* data, int length) {
		using var stream = new UnmanagedMemoryStream(data, length);
		return HasherXX.ComputeHash(stream).Hash.HashXXCompute();
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong HashXX(this byte[] data, int start, int length) {
		using var stream = new MemoryStream(data, start, length);
		return HasherXX.ComputeHash(stream).Hash.HashXXCompute();
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong HashXX(this Stream stream) {
		return HasherXX.ComputeHash(stream).Hash.HashXXCompute();
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong HashXX(this MemoryStream stream) {
		return HasherXX.ComputeHash(stream).Hash.HashXXCompute();
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong HashXX(this UnmanagedMemoryStream stream) {
		return HasherXX.ComputeHash(stream).Hash.HashXXCompute();
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	[Obsolete("Unoptimized")]
	internal static ulong HashXX<T>(this T[] data) where T : unmanaged => data.CastAs<T, byte>().HashXX();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong HashXX<T>(this in FixedSpan<T> data) where T : unmanaged {
		using var byteSpan = data.As<byte>();
		return byteSpan.HashXX();
	}
}
