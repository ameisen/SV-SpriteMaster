using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster;

static partial class Hashing {
	private const ulong FNV1Hash = 0XCBF29CE484222325UL;
	private const ulong FNV1Prime = 0X100000001B3UL;

	// FNV-1a hash.
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong HashFNV1(this byte[] data) {
		ulong hash = FNV1Hash;
		foreach (byte octet in data) {
			hash ^= octet;
			hash *= FNV1Prime;
		}

		return hash;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static ulong HashFNV1(this in FixedSpan<byte> data) {
		ulong hash = FNV1Hash;
		foreach (byte octet in data) {
			hash ^= octet;
			hash *= FNV1Prime;
		}

		return hash;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	[Obsolete("Unoptimized")]
	internal static ulong HashFNV1(this byte[] data,/* int start,*/ int length) => new FixedSpan<byte>(data, /*start, */length).HashFNV1();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	[Obsolete("Unoptimized")]
	internal static ulong HashFNV1<T>(this T[] data) where T : unmanaged => data.CastAs<T, byte>().HashFNV1();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static unsafe ulong HashFNV1<T>(this in FixedSpan<T> data) where T : unmanaged {
		using var byteSpan = data.As<byte>();
		return byteSpan.HashFNV1();
	}
}
