using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Hashing.Algorithms;

internal static unsafe partial class XxHash3 {

	[MethodImpl(Inline)]
	private static void StripeCopyTo64Scalar(this ReadOnlySpan<byte> source, Span<byte> destination) {
		source.Slice(0, 64).CopyTo(destination.Slice(0, 64));
	}

	[MethodImpl(Inline)]
	private static void StripeCopyTo128Scalar(this ReadOnlySpan<byte> source, Span<byte> destination) {
		source.Slice(0, 128).CopyTo(destination.Slice(0, 128));
	}

	// xxh3_accumulate_512_scalar
	[MethodImpl(Inline)]
	private static void Accumulate512Scalar(ulong* accumulator, byte* data, byte* secret) {
		PrefetchNonTemporalNext(data);
		PrefetchNonTemporalNext(secret);
		PrefetchNext(accumulator);

		AccumulatorBytes.AssertEqual(8u);

		Accumulate512ScalarPass(ref accumulator[0 ^ 1], ref accumulator[0], data + 0x00, secret + 0x00);
		Accumulate512ScalarPass(ref accumulator[1 ^ 1], ref accumulator[1], data + 0x08, secret + 0x08);
		Accumulate512ScalarPass(ref accumulator[2 ^ 1], ref accumulator[2], data + 0x10, secret + 0x10);
		Accumulate512ScalarPass(ref accumulator[3 ^ 1], ref accumulator[3], data + 0x18, secret + 0x18);
		Accumulate512ScalarPass(ref accumulator[4 ^ 1], ref accumulator[4], data + 0x20, secret + 0x20);
		Accumulate512ScalarPass(ref accumulator[5 ^ 1], ref accumulator[5], data + 0x28, secret + 0x28);
		Accumulate512ScalarPass(ref accumulator[6 ^ 1], ref accumulator[6], data + 0x30, secret + 0x30);
		Accumulate512ScalarPass(ref accumulator[7 ^ 1], ref accumulator[7], data + 0x38, secret + 0x38);
	}

	[MethodImpl(Inline)]
	private static void Accumulate512ScalarPass(ref ulong accumulator0, ref ulong accumulator1, byte* data, byte* secret) {
		ulong dataVal = LoadLittle64(data);
		ulong dataKey = dataVal ^ LoadLittle64(secret);

		accumulator0 += dataVal;
		accumulator1 += (uint)dataKey * (ulong)(uint)(dataKey >> 32);
	}

	// xxh3_scramble_acc_scalar
	[MethodImpl(Inline)]
	private static void ScrambleAccumulatorScalar(ulong* accumulator, byte* secret) {
		AccumulatorBytes.AssertEqual(8u);

		ScrambleAccumulatorScalarPass(ref accumulator[0], secret + 0x00);
		ScrambleAccumulatorScalarPass(ref accumulator[1], secret + 0x08);
		ScrambleAccumulatorScalarPass(ref accumulator[2], secret + 0x10);
		ScrambleAccumulatorScalarPass(ref accumulator[3], secret + 0x18);
		ScrambleAccumulatorScalarPass(ref accumulator[4], secret + 0x20);
		ScrambleAccumulatorScalarPass(ref accumulator[5], secret + 0x28);
		ScrambleAccumulatorScalarPass(ref accumulator[6], secret + 0x30);
		ScrambleAccumulatorScalarPass(ref accumulator[7], secret + 0x38);
	}

	[MethodImpl(Inline)]
	private static void ScrambleAccumulatorScalarPass(ref ulong accumulator, byte* secret) {
		ulong key64 = LoadLittle64(secret);
		ulong acc64 = accumulator;

		acc64 ^= acc64 >> 47;
		acc64 ^= key64;
		acc64 *= Prime32.Prime0;

		accumulator = acc64;
	}

}