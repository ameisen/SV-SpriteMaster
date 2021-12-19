using SpriteMaster.Types;
using System;
using System.Data.HashFunction.xxHash;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions {
	using XRectangle = Microsoft.Xna.Framework.Rectangle;

	internal static class _HashValues {
		internal const ulong Default = 0ul;
	}

	internal static class Hash {
		internal const ulong Default = _HashValues.Default;

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong Accumulate(ulong hash, ulong hashend) => hash ^ (hashend + 0x9e3779b9ul + (hash << 6) + (hash >> 2));

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong Accumulate (ulong hash, int hashend) => Accumulate(hash, unchecked((ulong)hashend));

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong Combine (params ulong[] hashes) {
			unchecked {
				ulong hash = 0;
				foreach (var subHash in hashes) {
					hash = Accumulate(hash, subHash);
				}
				return hash;
			}
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong Combine (params object[] hashes) {
			unchecked {
				ulong hash = 0;

				foreach (var subHash in hashes) {
					hash = subHash switch {
						long i => Accumulate(hash, (ulong)i),
						ulong i => Accumulate(hash, i),
						_ => Accumulate(hash, subHash.GetHashCode()),
					};
				}
				return hash;
			}
		}
	}

	internal static class Hashing {
		internal const ulong Default = _HashValues.Default;

		// FNV-1a hash.
		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong HashFNV1 (this byte[] data) => new FixedSpan<byte>(data).HashFNV1();

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong HashFNV1 (this in FixedSpan<byte> data) {
			const ulong prime = 0x100000001b3;
			ulong hash = 0xcbf29ce484222325;
			foreach (byte octet in data) {
				hash ^= octet;
				hash *= prime;
			}

			return hash;
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong HashFNV1 (this byte[] data,/* int start,*/ int length) => new FixedSpan<byte>(data, /*start, */length).HashFNV1();

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong HashFNV1<T> (this T[] data) where T : unmanaged => data.CastAs<T, byte>().HashFNV1();

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static unsafe ulong HashFNV1<T>(this in FixedSpan<T> data) where T : unmanaged {
			using var byteSpan = data.As<byte>();
			return byteSpan.HashFNV1();
		}

		private static readonly IxxHash HasherXX = xxHashFactory.Instance.Create(new xxHashConfig() { HashSizeInBits = 64 });

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		private static ulong HashXXCompute (this byte[] hashData) => BitConverter.ToUInt64(hashData, 0);

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong HashXX (this byte[] data) => HasherXX.ComputeHash(data).Hash.HashXXCompute();

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static unsafe ulong HashXX (this in FixedSpan<byte> data) {
			fixed (byte* p = &data.GetPinnableReference()) {
				using var stream = new UnmanagedMemoryStream(p, data.Length);
				return HasherXX.ComputeHash(stream).Hash.HashXXCompute();
			}
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong HashXX (this byte[] data, int start, int length) {
			using var stream = new MemoryStream(data, start, length);
			return HasherXX.ComputeHash(stream).Hash.HashXXCompute();
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong HashXX(this Stream stream) {
			return HasherXX.ComputeHash(stream).Hash.HashXXCompute();
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong HashXX(this MemoryStream stream) {
			return HasherXX.ComputeHash(stream).Hash.HashXXCompute();
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong HashXX<T> (this T[] data) where T : unmanaged => data.CastAs<T, byte>().HashXX();

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong HashXX<T>(this in FixedSpan<T> data) where T : unmanaged {
			using var byteSpan = data.As<byte>();
			return byteSpan.HashXX();
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong Hash (this byte[] data) => data.HashXX();//return data.HashFNV1();

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong Hash (this in FixedSpan<byte> data) => data.HashXX();

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong Hash (this byte[] data, int start, int length) => data.HashXX(start, length);//return data.HashFNV1();

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong Hash<T> (this T[] data) where T : unmanaged => data.HashXX();

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong Hash<T> (this in FixedSpan<T> data) where T : unmanaged => data.HashXX();

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong Hash(this Stream stream) => stream.HashXX();

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong Hash(this MemoryStream stream) => stream.HashXX();

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong Hash (this in Rectangle rectangle) =>
			((ulong)rectangle.X & 0xFFFF) |
			(((ulong)rectangle.Y & 0xFFFF) << 16) |
			(((ulong)rectangle.Width & 0xFFFF) << 32) |
			(((ulong)rectangle.Height & 0xFFFF) << 48);

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong Hash (this in XRectangle rectangle) =>
			((ulong)rectangle.X & 0xFFFF) |
			(((ulong)rectangle.Y & 0xFFFF) << 16) |
			(((ulong)rectangle.Width & 0xFFFF) << 32) |
			(((ulong)rectangle.Height & 0xFFFF) << 48);

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static ulong Hash (this in Bounds rectangle) =>
			((ulong)rectangle.X & 0xFFFF) |
			(((ulong)rectangle.Y & 0xFFFF) << 16) |
			(((ulong)rectangle.Width & 0xFFFF) << 32) |
			(((ulong)rectangle.Height & 0xFFFF) << 48);
	}
}
