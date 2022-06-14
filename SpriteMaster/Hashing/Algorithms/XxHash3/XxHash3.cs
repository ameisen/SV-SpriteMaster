﻿using Microsoft.Toolkit.HighPerformance;
using SpriteMaster.Extensions;
using System;
using System.Buffers.Binary;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace SpriteMaster.Hashing.Algorithms;

// https://github.com/Crauzer/XXHash3.NET/tree/main/XXHash3.NET
internal static unsafe partial class XxHash3 {
	private static class Span2DReflect<TSpanElement> where TSpanElement : unmanaged {
		internal delegate int GetStrideDelegate(ReadOnlySpan2D<TSpanElement> span);

		internal static readonly GetStrideDelegate GetStride =
			typeof(ReadOnlySpan2D<TSpanElement>).GetFieldGetterDelegate<GetStrideDelegate>("stride") ??
				throw new NullReferenceException($"{nameof(ReadOnlySpan2D<TSpanElement>)}.stride");
	}

	[Pure]
	[MethodImpl(Inline)]
	internal static ulong Hash64<T>(ReadOnlySpan2D<T> data) where T : unmanaged {
		if (data.TryGetSpan(out var span)) {
			return Hash64(span.AsBytes());
		}

		// TODO : there must be a better way to do this...

		ref byte underlyingByte = ref Unsafe.AsRef<byte>(Unsafe.AsPointer(ref data.DangerousGetReference()));
		int width = data.Width * sizeof(T);
		var castSpan = Span2D<byte>.DangerousCreate(
			value: ref underlyingByte,
			height: data.Height,
			width: width,
			pitch: (Span2DReflect<T>.GetStride(data) * sizeof(T)) - width
		);
		return Hash64(castSpan);
	}

	[Pure]
	[MethodImpl(Inline)]
	internal static ulong Hash64(ReadOnlySpan2D<byte> data) {
		if (data.TryGetSpan(out var span)) {
			return Hash64(span);
		}

		return Hash64(new SegmentedSpan(data));
	}

	[Pure]
	[MethodImpl(Inline)]
	private static ulong Hash64(SegmentedSpan data) {
		uint length = data.Length;

		return length switch {
			<= 16 => Hash0To16(data),
			<= 128 => Hash17To128(data),
			<= 240 => Hash129To240(data),
			_ => HashLong(data)
		};
	}

	[Pure]
	[MethodImpl(Inline)]
	internal static ulong Hash64(string data) =>
		Hash64(data.AsSpan().AsBytes());

	[Pure]
	[MethodImpl(Inline)]
	internal static ulong Hash64(ReadOnlySpan<byte> data) {
		uint length = (uint)data.Length;

		if (length <= 16) {
			return Hash0To16(ref MemoryMarshal.GetReference(data), (uint)data.Length);
		}

		if (length <= 128) {
			return Hash17To128(ref MemoryMarshal.GetReference(data), (uint)data.Length);
		}

		fixed (byte* dataPtr = data) {
			return Hash64N16(dataPtr, length);
		}
	}

	[Pure]
	[MethodImpl(Inline)]
	internal static ulong Hash64(byte* data, int length) =>
		Hash64(data, (uint)length);

	[Pure]
	[MethodImpl(Inline)]
	internal static ulong Hash64(byte* data, uint length) {
		return length switch {
			<= 16 => Hash0To16(ref Unsafe.AsRef<byte>(data), length),
			<= 128 => Hash17To128(ref Unsafe.AsRef<byte>(data), length),
			<= 240 => Hash129To240(data, length),
			_ => HashLong(data, length)
		};
	}

	[Pure]
	[MethodImpl(Inline)]
	private static ulong Hash64N16(byte* data, uint length) {
		return length switch {
			<= 240 => Hash129To240(data, length),
			_ => HashLong(data, length)
		};
	}

	private static readonly ulong ZeroLengthResult = AvalancheX64(SecretValues64.Secret38 ^ SecretValues64.Secret40);

	// XXH3_len_0to16_64b
	[Pure]
	[MethodImpl(Inline)]
	private static ulong Hash0To16(ref byte data, uint length) {
		return length switch {
			>= 9 => Hash9To16(ref data, length),
			>= 4 => Hash4To8(ref data, length),
			>= 1 => Hash1To3(ref data, length),
			_ => ZeroLengthResult
		};
	}

	// xxh3_len_9to16_64
	[Pure]
	[MethodImpl(Inline)]
	private static ulong Hash9To16(ref byte data, uint length) {
		const ulong bitFlip1 = SecretValues64.Secret18 ^ SecretValues64.Secret20;
		const ulong bitFlip2 = SecretValues64.Secret28 ^ SecretValues64.Secret30;
		ulong inputLow = data.Read<ulong>() ^ bitFlip1;
		ulong inputHigh = data.Read<ulong>(length - 8) ^ bitFlip2;
		ulong accumulator =
			(ulong)length +
			BinaryPrimitives.ReverseEndianness(inputLow) +
			inputHigh +
			Mul128Fold64(inputLow, inputHigh);

		return Avalanche(accumulator);
	}

	// xxh3_len_4to8_64
	[Pure]
	[MethodImpl(Inline)]
	private static ulong Hash4To8(ref byte data, uint length) {
		uint input1 = data.Read<uint>();
		uint input2 = data.Read<uint>(length - 4);
		const ulong bitFlip = SecretValues64.Secret08 ^ SecretValues64.Secret10;
		ulong input64 = input2 + ((ulong)input1 << 0x20);
		ulong keyed = input64 ^ bitFlip;

		return RotRotMulXorMulXor(keyed, (ulong)length);
	}

	// xxh3_len_1to3_64
	[Pure]
	[MethodImpl(Inline)]
	private static ulong Hash1To3(ref byte data, uint length) {
		byte c1 = data.Read<byte>();
		byte c2 = data.Read<byte>(length >> 1);
		byte c3 = data.Read<byte>(length - 1);
		uint combined = ((uint)c1 << 16) | ((uint)c2 << 24) | ((uint)c3 << 0) | ((uint)length << 8);
		const ulong bitFlip = SecretValues32.Secret00 ^ SecretValues32.Secret04;
		ulong keyed = combined ^ bitFlip;

		return AvalancheX64(keyed);
	}

	[StructLayout(LayoutKind.Sequential, Pack = 16, Size = 16)]
	private struct Data16 {
		internal readonly Span<byte> Span => new(Unsafe.AsPointer(ref Unsafe.AsRef(this)), 16);
		internal readonly ref byte Reference => ref MemoryMarshal.GetReference(Span);
	}

	[Pure]
	[MethodImpl(Inline)]
	private static ulong Hash0To16(SegmentedSpan data) {
		Data16 localData = default;
		data.Source.CopyTo(localData.Span);
		return Hash0To16(ref localData.Reference, data.Length);
	}

	// xxh3_17to128_64
	[Pure]
	[MethodImpl(Hot)]
	private static ulong Hash17To128(ref byte data, uint length) {
		var accumulator = length * Prime64.Prime0;

		if (length > 0x20) {
			if (length > 0x40) {
				if (length > 0x60) {
					accumulator += Mix16(ref data.Offset(0x30), SecretValues64.Secret60, SecretValues64.Secret68);
					accumulator += Mix16(ref data.Offset(length - 0x40), SecretValues64.Secret70, SecretValues64.Secret78);
				}

				accumulator += Mix16(ref data.Offset(0x20), SecretValues64.Secret40, SecretValues64.Secret48);
				accumulator += Mix16(ref data.Offset(length - 0x30), SecretValues64.Secret50, SecretValues64.Secret58);
			}

			accumulator += Mix16(ref data.Offset(0x10), SecretValues64.Secret20, SecretValues64.Secret28);
			accumulator += Mix16(ref data.Offset(length - 0x20), SecretValues64.Secret30, SecretValues64.Secret38);
		}

		accumulator += Mix16(ref data, SecretValues64.Secret00, SecretValues64.Secret08);
		accumulator += Mix16(ref data.Offset(length - 0x10), SecretValues64.Secret10, SecretValues64.Secret18);

		return Avalanche(accumulator);
	}

	[Pure]
	[MethodImpl(Hot)]
	private static ulong Hash17To128(SegmentedSpan data) {
		uint length = data.Length;
		var accumulator = length * Prime64.Prime0;

		Span<byte> tempBuffer = stackalloc byte[16];

		if (length > 0x20) {
			if (length > 0x40) {
				if (length > 0x60) {
					accumulator += Mix16(ref data.Slice(tempBuffer, 0x30, 16).AsRef(), SecretValues64.Secret60, SecretValues64.Secret68);
					accumulator += Mix16(ref data.Slice(tempBuffer, length - 0x40, 16).AsRef(), SecretValues64.Secret70, SecretValues64.Secret78);
				}

				accumulator += Mix16(ref data.Slice(tempBuffer, 0x20, 16).AsRef(), SecretValues64.Secret40, SecretValues64.Secret48);
				accumulator += Mix16(ref data.Slice(tempBuffer, length - 0x30, 16).AsRef(), SecretValues64.Secret50, SecretValues64.Secret58);
			}

			accumulator += Mix16(ref data.Slice(tempBuffer, 0x10, 16).AsRef(), SecretValues64.Secret20, SecretValues64.Secret28);
			accumulator += Mix16(ref data.Slice(tempBuffer, length - 0x20, 16).AsRef(), SecretValues64.Secret30, SecretValues64.Secret38);
		}

		accumulator += Mix16(ref data.Slice(tempBuffer, 0, 16).AsRef(), SecretValues64.Secret00, SecretValues64.Secret08);
		accumulator += Mix16(ref data.Slice(tempBuffer, length - 0x10, 16).AsRef(), SecretValues64.Secret10, SecretValues64.Secret18);

		return Avalanche(accumulator);
	}

	// xxh3_129to240_64
	[Pure]
	[MethodImpl(Hot)]
	private static ulong Hash129To240(byte* data, uint length) {
		byte* secret = Secret;

		PrefetchNonTemporalNext(data);
		PrefetchNonTemporalNext(secret);

		var accumulator = length * Prime64.Prime0;

		accumulator += Mix16(data + 0x00, SecretValues64.Secret00, SecretValues64.Secret08);
		accumulator += Mix16(data + 0x10, SecretValues64.Secret10, SecretValues64.Secret18);
		accumulator += Mix16(data + 0x20, SecretValues64.Secret20, SecretValues64.Secret28);
		accumulator += Mix16(data + 0x30, SecretValues64.Secret30, SecretValues64.Secret38);
		accumulator += Mix16(data + 0x40, SecretValues64.Secret40, SecretValues64.Secret48);
		accumulator += Mix16(data + 0x50, SecretValues64.Secret50, SecretValues64.Secret58);
		accumulator += Mix16(data + 0x60, SecretValues64.Secret60, SecretValues64.Secret68);
		accumulator += Mix16(data + 0x70, SecretValues64.Secret70, SecretValues64.Secret78);

		accumulator = Avalanche(accumulator);

		uint roundCount = (length / 0x10) - 8;
		// min is 129, thus min roundCount is 8, max is 15
		// the 8 are handled above.

		var offsetData = data + 0x80;
		for (uint i = 0u; i < roundCount; ++i) {
			accumulator += Mix16(offsetData + (0x10 * i), secret + (0x10 * i) + 3);
		}

		accumulator += Mix16(data + (length - 0x10), SecretValues64.Secret77, SecretValues64.Secret7F);

		return Avalanche(accumulator);
	}

	[Pure]
	[MethodImpl(Hot)]
	private static ulong Hash129To240(SegmentedSpan data) {
		byte* secret = Secret;

		uint length = data.Length;
		
		PrefetchNonTemporalNext(secret);

		var accumulator = length * Prime64.Prime0;

		Span<byte> tempBuffer = stackalloc byte[16];

		accumulator += Mix16(ref data.Slice(tempBuffer, 0x00, 16).AsRef(), SecretValues64.Secret00, SecretValues64.Secret08);
		accumulator += Mix16(ref data.Slice(tempBuffer, 0x10, 16).AsRef(), SecretValues64.Secret10, SecretValues64.Secret18);
		accumulator += Mix16(ref data.Slice(tempBuffer, 0x20, 16).AsRef(), SecretValues64.Secret20, SecretValues64.Secret28);
		accumulator += Mix16(ref data.Slice(tempBuffer, 0x30, 16).AsRef(), SecretValues64.Secret30, SecretValues64.Secret38);
		accumulator += Mix16(ref data.Slice(tempBuffer, 0x40, 16).AsRef(), SecretValues64.Secret40, SecretValues64.Secret48);
		accumulator += Mix16(ref data.Slice(tempBuffer, 0x50, 16).AsRef(), SecretValues64.Secret50, SecretValues64.Secret58);
		accumulator += Mix16(ref data.Slice(tempBuffer, 0x60, 16).AsRef(), SecretValues64.Secret60, SecretValues64.Secret68);
		accumulator += Mix16(ref data.Slice(tempBuffer, 0x70, 16).AsRef(), SecretValues64.Secret70, SecretValues64.Secret78);

		accumulator = Avalanche(accumulator);

		uint roundCount = (length / 0x10) - 8;
		// min is 129, thus min roundCount is 8, max is 15
		// the 8 are handled above.

		for (uint i = 0u; i < roundCount; ++i) {
			accumulator += Mix16(ref data.Slice(tempBuffer, 0x80 + (0x10 * i), 16).AsRef(), secret + (0x10 * i) + 3);
		}

		accumulator += Mix16(ref data.Slice(tempBuffer, length - 0x10, 16).AsRef(), SecretValues64.Secret77, SecretValues64.Secret7F);

		return Avalanche(accumulator);
	}

	// xxh3_hashLong_64
	[Pure]
	[MethodImpl(Inline)]
	private static ulong HashLong(byte* data, uint length) {
		// Common Sizes:
		// 16x16 = 1024 bytes
		// 16x32 = 2048 bytes

		if ((Avx2.IsSupported && UseAVX2) || (Sse2.IsSupported && UseSSE2)) {
			// Checks if it's a power of two
			if (BitOperations.PopCount(length) == 1) {
				var offset = (uint)BitOperations.TrailingZeroCount(length);
				switch (offset) {
					case 8:
						return HashLongFixed(data, 0x100);
					case 9:
						return HashLongFixed(data, 0x200);
					case 10:
						return HashLongFixed(data, 0x400);
					case 11:
						return HashLongFixed(data, 0x800);
					case 12:
						return HashLongFixed(data, 0x1000);
					case 13:
						return HashLongFixed(data, 0x2000);
					case 14:
						return HashLongFixed(data, 0x4000);
					case 15:
						return HashLongFixed(data, 0x8000);
					case 16:
						return HashLongFixed(data, 0x10000);
					case 17:
						return HashLongFixed(data, 0x20000);
					case 18:
						return HashLongFixed(data, 0x40000);
					case 19:
						return HashLongFixed(data, 0x80000);
					case 20:
						return HashLongFixed(data, 0x100000);
					case 21:
						return HashLongFixed(data, 0x200000);
					case 22:
						return HashLongFixed(data, 0x400000);
					case 23:
						return HashLongFixed(data, 0x800000);
					case 24:
						return HashLongFixed(data, 0x1000000);
					case 25:
						return HashLongFixed(data, 0x2000000);
					case 26:
						return HashLongFixed(data, 0x4000000);
					case 27:
						return HashLongFixed(data, 0x8000000);
					case 28:
						return HashLongFixed(data, 0x10000000);
					case 29:
						return HashLongFixed(data, 0x20000000);
					case 30:
						return HashLongFixed(data, 0x40000000);
					case 31:
						return HashLongFixed(data, 0x80000000);
				}
			}

			return HashLongFixed(data, length);
		}

		var accumulatorStore = new Accumulator();
		ulong* accumulator = accumulatorStore.Data;

		byte* secret = Secret;

		const uint stripesPerBlock = (SecretLength - StripeLength) / 8u;
		stripesPerBlock.AssertEqual(16u);
		const uint blockLength = StripeLength * stripesPerBlock;
		blockLength.AssertEqual(1024u);
		uint blockCount = (length - 1) / blockLength;

		// To get here, length must be greater than 240.
		// Thus, it's possible that we end up skipping these blocks altogether

		uint block = 0;

		for (; block < blockCount; ++block) {
			Accumulate(accumulator, data + (block * blockLength), secret, stripesPerBlock);
			ScrambleAccumulator(accumulator, secret + (SecretLength - StripeLength));
		}

		uint stripeCount = (length - 1u - (blockLength * blockCount)) / StripeLength;
		Accumulate(accumulator, data + (blockCount * blockLength), secret, stripeCount);

		byte* p = data + (length - StripeLength);
		Accumulate512(accumulator, p, secret + (SecretLength - StripeLength - 7u));

		return MergeAccumulators(ref accumulatorStore, length * Prime64.Prime0);
	}

	[Pure]
	[MethodImpl(Inline)]
	private static ulong HashLong(SegmentedSpan data) {
		uint length = data.Length;

		// Common Sizes:
		// 16x16 = 1024 bytes
		// 16x32 = 2048 bytes

		if ((Avx2.IsSupported && UseAVX2) || (Sse2.IsSupported && UseSSE2)) {
			// Checks if it's a power of two
			if (BitOperations.PopCount(length) == 1) {
				var offset = (uint)BitOperations.TrailingZeroCount(length);
				switch (offset) {
					case 8:
						return HashLongFixed(data, 0x100);
					case 9:
						return HashLongFixed(data, 0x200);
					case 10:
						return HashLongFixed(data, 0x400);
					case 11:
						return HashLongFixed(data, 0x800);
					case 12:
						return HashLongFixed(data, 0x1000);
					case 13:
						return HashLongFixed(data, 0x2000);
					case 14:
						return HashLongFixed(data, 0x4000);
					case 15:
						return HashLongFixed(data, 0x8000);
					case 16:
						return HashLongFixed(data, 0x10000);
					case 17:
						return HashLongFixed(data, 0x20000);
					case 18:
						return HashLongFixed(data, 0x40000);
					case 19:
						return HashLongFixed(data, 0x80000);
					case 20:
						return HashLongFixed(data, 0x100000);
					case 21:
						return HashLongFixed(data, 0x200000);
					case 22:
						return HashLongFixed(data, 0x400000);
					case 23:
						return HashLongFixed(data, 0x800000);
					case 24:
						return HashLongFixed(data, 0x1000000);
					case 25:
						return HashLongFixed(data, 0x2000000);
					case 26:
						return HashLongFixed(data, 0x4000000);
					case 27:
						return HashLongFixed(data, 0x8000000);
					case 28:
						return HashLongFixed(data, 0x10000000);
					case 29:
						return HashLongFixed(data, 0x20000000);
					case 30:
						return HashLongFixed(data, 0x40000000);
					case 31:
						return HashLongFixed(data, 0x80000000);
				}
			}

			return HashLongFixed(data, length);
		}

		// TODO : should do this faster, but we only run on systems that support SIMD...
		Span<byte> tempData = SpanExt.Make<byte>((int)length);
		data.Source.CopyTo(tempData);

		fixed (byte* ptr = tempData) {
			return HashLong(ptr, length);
		}
	}

	[Pure]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static ulong HashLongFixed(byte* data, uint length) {
		if (Avx2.IsSupported && UseAVX2) {
			return HashLongAvx2(data, length);
		}

		(Sse2.IsSupported && UseSSE2).AssertTrue();

		return HashLongSse2(data, length);
	}

	[Pure]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static ulong HashLongFixed(SegmentedSpan data, uint length) {
		if (Avx2.IsSupported && UseAVX2) {
			return HashLongAvx2(data, length);
		}

		(Sse2.IsSupported && UseSSE2).AssertTrue();

		return HashLongSse2(data, length);
	}

	[MethodImpl(Inline)]
	private static void StripeCopyTo64(this ReadOnlySpan<byte> source, Span<byte> destination) {
		if (Avx2.IsSupported && UseAVX2) {
			StripeCopyTo64Avx2(source, destination);
			return;
		}

		if (Sse2.IsSupported && UseSSE2) {
			StripeCopyTo64Sse2(source, destination);
			return;
		}

		StripeCopyTo64Scalar(source, destination);
	}

	[MethodImpl(Inline)]
	private static void StripeCopyTo128(this ReadOnlySpan<byte> source, Span<byte> destination) {
		if (Avx2.IsSupported && UseAVX2) {
			StripeCopyTo128Avx2(source, destination);
			return;
		}

		if (Sse2.IsSupported && UseSSE2) {
			StripeCopyTo128Sse2(source, destination);
			return;
		}

		StripeCopyTo128Scalar(source, destination);
	}

	[Pure]
	[MethodImpl(Inline)]
	private static ulong MergeAccumulators(ref Accumulator accumulator, ulong start) {
		ulong result = start;

		if (Avx2.IsSupported && UseAVX2) {
			ref var accumulator256 = ref accumulator.Data256;

			var data0 = Avx2.Xor(accumulator256.Data0, Vector256.Create(SecretValues64.Secret0B, SecretValues64.Secret13, SecretValues64.Secret1B, SecretValues64.Secret23));
			var data1 = Avx2.Xor(accumulator256.Data1, Vector256.Create(SecretValues64.Secret2B, SecretValues64.Secret33, SecretValues64.Secret3B, SecretValues64.Secret43));

			result += MixAccumulators(data0.GetElement(0), data0.GetElement(1));
			result += MixAccumulators(data0.GetElement(2), data0.GetElement(3));
			result += MixAccumulators(data1.GetElement(0), data1.GetElement(1));
			result += MixAccumulators(data1.GetElement(2), data1.GetElement(3));
		}
		else if (Sse2.IsSupported && UseSSE2) {
			ref var accumulator128 = ref accumulator.Data128;

			var data0 = Sse2.Xor(accumulator128.Data0, Vector128.Create(SecretValues64.Secret0B, SecretValues64.Secret13));
			var data1 = Sse2.Xor(accumulator128.Data0, Vector128.Create(SecretValues64.Secret1B, SecretValues64.Secret23));
			var data2 = Sse2.Xor(accumulator128.Data1, Vector128.Create(SecretValues64.Secret2B, SecretValues64.Secret33));
			var data3 = Sse2.Xor(accumulator128.Data1, Vector128.Create(SecretValues64.Secret3B, SecretValues64.Secret43));

			result += MixAccumulators(data0.GetElement(0), data0.GetElement(1));
			result += MixAccumulators(data1.GetElement(0), data1.GetElement(1));
			result += MixAccumulators(data2.GetElement(0), data2.GetElement(1));
			result += MixAccumulators(data3.GetElement(0), data3.GetElement(1));
		}
		else if (AdvSimd.IsSupported && UseNeon) {
			ref var accumulator128 = ref accumulator.Data128;

			var data0 = AdvSimd.Xor(accumulator128.Data0, Vector128.Create(SecretValues64.Secret0B, SecretValues64.Secret13));
			var data1 = AdvSimd.Xor(accumulator128.Data0, Vector128.Create(SecretValues64.Secret1B, SecretValues64.Secret23));
			var data2 = AdvSimd.Xor(accumulator128.Data1, Vector128.Create(SecretValues64.Secret2B, SecretValues64.Secret33));
			var data3 = AdvSimd.Xor(accumulator128.Data1, Vector128.Create(SecretValues64.Secret3B, SecretValues64.Secret43));

			result += MixAccumulators(data0.GetElement(0), data0.GetElement(1));
			result += MixAccumulators(data1.GetElement(0), data1.GetElement(1));
			result += MixAccumulators(data2.GetElement(0), data2.GetElement(1));
			result += MixAccumulators(data3.GetElement(0), data3.GetElement(1));
		}
		else {
			result += MixAccumulators(accumulator.Data[0], accumulator.Data[1], SecretValues64.Secret0B, SecretValues64.Secret13);
			result += MixAccumulators(accumulator.Data[2], accumulator.Data[3], SecretValues64.Secret1B, SecretValues64.Secret23);
			result += MixAccumulators(accumulator.Data[4], accumulator.Data[5], SecretValues64.Secret2B, SecretValues64.Secret33);
			result += MixAccumulators(accumulator.Data[6], accumulator.Data[7], SecretValues64.Secret3B, SecretValues64.Secret43);
		}

		return Avalanche(result);
	}

	[Pure]
	[MethodImpl(Inline)]
	private static ulong MixAccumulators(ulong accumulator0, ulong accumulator1, ulong secretLo, ulong secretHi) {
		return Mul128Fold64(
			accumulator0 ^ secretLo,
			accumulator1 ^ secretHi
		);
	}

	[Pure]
	[MethodImpl(Inline)]
	private static ulong MixAccumulators(ulong accumulator0, ulong accumulator1) {
		return Mul128Fold64(
			accumulator0,
			accumulator1
		);
	}

	// xxh3_avalanche
	[Pure]
	[MethodImpl(Inline)]
	private static ulong Avalanche(ulong hash) {
		hash ^= hash >> 37;
		hash *= 0x1656_6791_9E37_79F9UL;
		hash ^= hash >> 0x20;

		return hash;
	}

	// xxh64_avalanche
	[Pure]
	[MethodImpl(Inline)]
	private static ulong AvalancheX64(ulong hash) {
		hash ^= hash >> 33;
		hash *= Prime64.Prime1;
		hash ^= hash >> 29;
		hash *= Prime64.Prime2;
		hash ^= hash >> 0x20;

		return hash;
	}

	// xxh3_rrmxmx
	[Pure]
	[MethodImpl(Inline)]
	private static ulong RotRotMulXorMulXor(ulong h64, ulong len) {
		h64 ^= BitOperations.RotateLeft(h64, 49) ^ BitOperations.RotateLeft(h64, 24);
		h64 *= 0x9FB2_1C65_1E98_DF25UL;
		h64 ^= (h64 >> 35) + len;
		h64 *= 0x9FB2_1C65_1E98_DF25UL;

		return h64 ^ (h64 >> 28);
	}

	// xxh3_mix16B
	[Pure]
	[MethodImpl(Inline)]
	private static ulong Mix16(byte* data, byte* secret) {
		ulong inputLow = LoadLittle64(data);
		ulong inputHigh = LoadLittle64(data + 8);

		return Mul128Fold64(
			inputLow ^ LoadLittle64(secret),
			inputHigh ^ LoadLittle64(secret + 8)
		);
	}

	[Pure]
	[MethodImpl(Inline)]
	private static ulong Mix16(byte* data, ulong secretLo, ulong secretHi) {
		ulong inputLow = LoadLittle64(data);
		ulong inputHigh = LoadLittle64(data + 8);

		return Mul128Fold64(
			inputLow ^ secretLo,
			inputHigh ^ secretHi
		);
	}

	[Pure]
	[MethodImpl(Inline)]
	private static ulong Mix16(ref byte data, byte* secret) {
		ulong inputLow = data.Read<ulong>();
		ulong inputHigh = data.Read<ulong>(8);

		return Mul128Fold64(
			inputLow ^ LoadLittle64(secret),
			inputHigh ^ LoadLittle64(secret + 8)
		);
	}

	[Pure]
	[MethodImpl(Inline)]
	private static ulong Mix16(ref byte data, ulong secretLo, ulong secretHi) {
		ulong inputLow = data.Read<ulong>();
		ulong inputHigh = data.Read<ulong>(8);

		return Mul128Fold64(
			inputLow ^ secretLo,
			inputHigh ^ secretHi
		);
	}

	// xxh3_accumulate
	[Pure]
	[MethodImpl(Inline)]
	private static void Accumulate(ulong* accumulator, byte* data, byte* secret, uint stripeCount) {
		uint i = 0;
		if (Avx2.IsSupported && UseAVX2) {
			for (; i + 7u < stripeCount; i += 8u) {
				PrefetchNonTemporalNext(data + (i * StripeLength) + 0x040);
				PrefetchNonTemporalNext(data + (i * StripeLength) + 0x080);
				PrefetchNonTemporalNext(data + (i * StripeLength) + 0x100);
				PrefetchNonTemporalNext(data + (i * StripeLength) + 0x180);
				PrefetchNonTemporalNext(data + (i * StripeLength) + 0x200);
				PrefetchNonTemporalNext(data + (i * StripeLength) + 0x280);
				PrefetchNonTemporalNext(data + (i * StripeLength) + 0x200);
				PrefetchNonTemporalNext(data + (i * StripeLength) + 0x280);
				Accumulate1024Avx2(accumulator, data + ((i + 0) * StripeLength), secret + ((i + 0) * 8u));
				Accumulate1024Avx2(accumulator, data + ((i + 2) * StripeLength), secret + ((i + 2) * 8u));
				Accumulate1024Avx2(accumulator, data + ((i + 4) * StripeLength), secret + ((i + 4) * 8u));
				Accumulate1024Avx2(accumulator, data + ((i + 6) * StripeLength), secret + ((i + 6) * 8u));
			}

			for (; i + 1u < stripeCount; i += 2u) {
				PrefetchNonTemporalNext(data + (i * StripeLength) + 0x040);
				PrefetchNonTemporalNext(data + (i * StripeLength) + 0x080);
				Accumulate1024Avx2(accumulator, data + (i * StripeLength), secret + (i * 8u));
			}
		}
		for (; i < stripeCount; ++i) {
			PrefetchNonTemporalNext(data + (i * StripeLength) + 0x040);
			Accumulate512(accumulator, data + (i * StripeLength), secret + (i * 8u));
		}
	}

	// xxh3_accumulate_512
	[MethodImpl(Inline)]
	private static void Accumulate512(ulong* accumulator, byte* data, byte* secret) {
		if (Avx2.IsSupported && UseAVX2) {
			Accumulate512Avx2(accumulator, data, secret);
		}
		else if (Sse2.IsSupported && UseSSE2) {
			Accumulate512Sse2(accumulator, data, secret);
		}
		else if (AdvSimd.IsSupported) {
			Accumulate512Neon(accumulator, data, secret);
		}
		else {
			Accumulate512Scalar(accumulator, data, secret);
		}
	}

	// xxh3_scramble_acc
	[MethodImpl(Inline)]
	private static void ScrambleAccumulator(ulong* accumulator, byte* secret) {
		if (Avx2.IsSupported && UseAVX2) {
			ScrambleAccumulatorAvx2(accumulator, secret);
		}
		else if (Sse2.IsSupported && UseSSE2) {
			ScrambleAccumulatorSse2(accumulator, secret);
		}
		else if (AdvSimd.IsSupported) {
			ScrambleAccumulatorNeon(accumulator, secret);
		}
		else {
			ScrambleAccumulatorScalar(accumulator, secret);
		}
	}

	// xxh3_mul128_fold64
	[Pure]
	[MethodImpl(Inline)]
	private static ulong Mul128Fold64(ulong lhs, ulong rhs) {
		ulong low;
		ulong high = Bmi2.X64.IsSupported ?
			Bmi2.X64.MultiplyNoFlags(lhs, rhs, &low) :
			Math.BigMul(lhs, rhs, out low);
		return low ^ high;
	}
}