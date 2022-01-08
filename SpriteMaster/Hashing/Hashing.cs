using Microsoft.Toolkit.HighPerformance;
using SpriteMaster.Extensions;
using SpriteMaster.Types;

using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace SpriteMaster;

static partial class Hashing {
	internal const ulong Default = 0UL;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Accumulate(ulong hash, ulong hashend) => hash ^ hashend + 0x9e3779b9ul + (hash << 6) + (hash >> 2);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Accumulate(ulong hash, int hashend) => Accumulate(hash, (ulong)hashend);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Combine(params ulong[] hashes) {
		ulong hash = 0;
		foreach (var subHash in hashes) {
			hash = Accumulate(hash, subHash);
		}
		return hash;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Combine(params object[] hashes) {
		ulong hash = 0;

		foreach (var subHash in hashes) {
			hash = subHash switch {
				long i => Accumulate(hash, (ulong)i),
				ulong i => Accumulate(hash, i),
				string s => Accumulate(hash, s.GetSafeHash()),
				StringBuilder s => Accumulate(hash, s.ToString().GetSafeHash()),
				_ => Accumulate(hash, subHash.GetHashCode()),
			};
		}
		return hash;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Hash(this byte[] data) => data.HashXX3();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Hash(this ReadOnlySequence<byte> data) => data.HashXX3();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Hash(this Span2D<byte> data) => data.HashXX3();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Hash(this ReadOnlySpan<byte> data) => data.HashXX3();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Hash(this in ReadOnlyMemory<byte> data) => data.Span.HashXX3();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static unsafe ulong Hash(byte* data, int length) => HashXX3(data, length);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Hash(this byte[] data, int start, int length) => data.HashXX3(start, length);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Hash<T>(this T[] data) where T : unmanaged => data.HashXX3();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Hash(this Stream stream) => stream.HashXX3();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Hash(this MemoryStream stream) => stream.HashXX3();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Hash(this UnmanagedMemoryStream stream) => stream.HashXX3();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Hash(this in DrawingRectangle rectangle) =>
		(ulong)rectangle.X & 0xFFFF |
		((ulong)rectangle.Y & 0xFFFF) << 16 |
		((ulong)rectangle.Width & 0xFFFF) << 32 |
		((ulong)rectangle.Height & 0xFFFF) << 48;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Hash(this in XNA.Rectangle rectangle) =>
		(ulong)rectangle.X & 0xFFFF |
		((ulong)rectangle.Y & 0xFFFF) << 16 |
		((ulong)rectangle.Width & 0xFFFF) << 32 |
		((ulong)rectangle.Height & 0xFFFF) << 48;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong Hash(this in Bounds rectangle) =>
		(ulong)rectangle.X & 0xFFFF |
		((ulong)rectangle.Y & 0xFFFF) << 16 |
		((ulong)rectangle.Width & 0xFFFF) << 32 |
		((ulong)rectangle.Height & 0xFFFF) << 48;
}
