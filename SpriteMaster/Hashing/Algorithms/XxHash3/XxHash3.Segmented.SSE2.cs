using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SpriteMaster.Hashing.Algorithms;

internal static unsafe partial class XxHash3 {
	[MethodImpl(Inline)]
	private static void StripeCopyTo64Sse2(this ReadOnlySpan<byte> source, Span<byte> destination) {
		fixed (byte* src = source) {
			fixed (byte* dst = destination) {
				// 64B = 512b
				var source0 = Sse2.LoadVector128((ulong*)(src + 0x00));
				var source1 = Sse2.LoadVector128((ulong*)(src + 0x10));
				var source2 = Sse2.LoadVector128((ulong*)(src + 0x20));
				var source3 = Sse2.LoadVector128((ulong*)(src + 0x30));
				Sse2.Store((ulong*)(dst + 0x00), source0);
				Sse2.Store((ulong*)(dst + 0x10), source1);
				Sse2.Store((ulong*)(dst + 0x20), source2);
				Sse2.Store((ulong*)(dst + 0x30), source3);
			}
		}
	}

	[MethodImpl(Inline)]
	private static void StripeCopyTo128Sse2(this ReadOnlySpan<byte> source, Span<byte> destination) {
		fixed (byte* src = source) {
			fixed (byte* dst = destination) {
				// 64B = 512b
				var source0 = Sse2.LoadVector128((ulong*)(src + 0x00));
				var source1 = Sse2.LoadVector128((ulong*)(src + 0x10));
				var source2 = Sse2.LoadVector128((ulong*)(src + 0x20));
				var source3 = Sse2.LoadVector128((ulong*)(src + 0x30));
				var source4 = Sse2.LoadVector128((ulong*)(src + 0x40));
				var source5 = Sse2.LoadVector128((ulong*)(src + 0x50));
				var source6 = Sse2.LoadVector128((ulong*)(src + 0x60));
				var source7 = Sse2.LoadVector128((ulong*)(src + 0x70));
				Sse2.Store((ulong*)(dst + 0x00), source0);
				Sse2.Store((ulong*)(dst + 0x10), source1);
				Sse2.Store((ulong*)(dst + 0x20), source2);
				Sse2.Store((ulong*)(dst + 0x30), source3);
				Sse2.Store((ulong*)(dst + 0x40), source4);
				Sse2.Store((ulong*)(dst + 0x50), source5);
				Sse2.Store((ulong*)(dst + 0x60), source6);
				Sse2.Store((ulong*)(dst + 0x70), source7);
			}
		}
	}

	[Pure]
	[MethodImpl(Inline)]
	private static ulong HashLongSse2(SegmentedSpan span, uint length) {
		byte* secret = Secret;

		StripeLength.AssertEqual(64U);

		const uint stripesPerBlock = (SecretLength - StripeLength) / 8U;
		stripesPerBlock.AssertEqual(16u);
		const uint blockLength = StripeLength * stripesPerBlock;
		blockLength.AssertEqual(1024u);

		if (span.Width.IsAligned(blockLength)) {
			//return HashLongSse2PerBlock(span, length);
		}
		if (span.Width.IsAligned(StripeLength)) {
			return HashLongSse2PerStripe(span, length);
		}

		uint blocks = (length - 1) / blockLength;

		Vector128<ulong> accumulator0 = Vector128.Create(Prime32.Prime2, Prime64.Prime0);
		Vector128<ulong> accumulator1 = Vector128.Create(Prime64.Prime1, Prime64.Prime2);
		Vector128<ulong> accumulator2 = Vector128.Create(Prime64.Prime3, Prime32.Prime1);
		Vector128<ulong> accumulator3 = Vector128.Create(Prime64.Prime4, Prime32.Prime0);

		var primeVector = Vector128.Create(Prime32.Prime0);

		Span<byte> localData = stackalloc byte[256];
		fixed (byte* localDataPtr = localData) {
			uint block = 0;

			for (; block < blocks; ++block) {
				{
					uint lDataOffset = block * blockLength;
					byte* lSecret = secret;

					uint stripe = 0;

					for (; stripe + 1 < stripesPerBlock; stripe += 2) {
						{
							uint llDataOffset = lDataOffset + (stripe * StripeLength);
							byte* llSecret = lSecret + (stripe * 8u);

							Vector128<ulong> result0, result1, result2, result3;

							span.SliceTo(localData, llDataOffset);
							byte* llData = localDataPtr;

							{
								var dataVec0 = Sse2.LoadVector128(llData + 0x00u).AsUInt64();
								var dataVec1 = Sse2.LoadVector128(llData + 0x10u).AsUInt64();
								var dataVec2 = Sse2.LoadVector128(llData + 0x20u).AsUInt64();
								var dataVec3 = Sse2.LoadVector128(llData + 0x30u).AsUInt64();
								var keyVec0 = Sse2.LoadVector128(llSecret + 0x00u).AsUInt64();
								var keyVec1 = Sse2.LoadVector128(llSecret + 0x10u).AsUInt64();
								var keyVec2 = Sse2.LoadVector128(llSecret + 0x20u).AsUInt64();
								var keyVec3 = Sse2.LoadVector128(llSecret + 0x30u).AsUInt64();

								var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
								var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
								var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
								var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
								var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
								var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
								var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
								var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
								var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
								var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
								var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
								var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

								var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
								var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
								var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
								var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
								var addend0 = accumulator0;
								var addend1 = accumulator1;
								var addend2 = accumulator2;
								var addend3 = accumulator3;

								var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
								var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
								var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
								var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

								result0 = Sse2.Add(product0, sum0);
								result1 = Sse2.Add(product1, sum1);
								result2 = Sse2.Add(product2, sum2);
								result3 = Sse2.Add(product3, sum3);
							}

							{
								var dataVec0 = Sse2.LoadVector128(llData + 0x40u).AsUInt64();
								var dataVec1 = Sse2.LoadVector128(llData + 0x50u).AsUInt64();
								var dataVec2 = Sse2.LoadVector128(llData + 0x60u).AsUInt64();
								var dataVec3 = Sse2.LoadVector128(llData + 0x70u).AsUInt64();
								var keyVec0 = Sse2.LoadVector128(llSecret + 0x08u).AsUInt64();
								var keyVec1 = Sse2.LoadVector128(llSecret + 0x18u).AsUInt64();
								var keyVec2 = Sse2.LoadVector128(llSecret + 0x28u).AsUInt64();
								var keyVec3 = Sse2.LoadVector128(llSecret + 0x38u).AsUInt64();

								var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
								var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
								var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
								var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
								var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
								var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
								var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
								var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
								var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
								var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
								var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
								var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

								var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
								var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
								var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
								var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
								var addend0 = result0;
								var addend1 = result1;
								var addend2 = result2;
								var addend3 = result3;

								var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
								var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
								var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
								var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

								accumulator0 = Sse2.Add(product0, sum0);
								accumulator1 = Sse2.Add(product1, sum1);
								accumulator2 = Sse2.Add(product2, sum2);
								accumulator3 = Sse2.Add(product3, sum3);
							}
						}
					}

					if (stripe < stripesPerBlock) {
						{
							uint llDataOffset = lDataOffset + (stripe * StripeLength);
							byte* llSecret = lSecret + (stripe * 8u);

							span.SliceTo(localData.Slice(0, 0x40), llDataOffset);
							byte* llData = localDataPtr;

							var dataVec0 = Sse2.LoadVector128(llData + 0x00u).AsUInt64();
							var dataVec1 = Sse2.LoadVector128(llData + 0x10u).AsUInt64();
							var dataVec2 = Sse2.LoadVector128(llData + 0x20u).AsUInt64();
							var dataVec3 = Sse2.LoadVector128(llData + 0x30u).AsUInt64();
							var keyVec0 = Sse2.LoadVector128(llSecret + 0x00u).AsUInt64();
							var keyVec1 = Sse2.LoadVector128(llSecret + 0x10u).AsUInt64();
							var keyVec2 = Sse2.LoadVector128(llSecret + 0x20u).AsUInt64();
							var keyVec3 = Sse2.LoadVector128(llSecret + 0x30u).AsUInt64();

							var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
							var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
							var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
							var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
							var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
							var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
							var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
							var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
							var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
							var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
							var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
							var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

							var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
							var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
							var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
							var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
							var addend0 = accumulator0;
							var addend1 = accumulator1;
							var addend2 = accumulator2;
							var addend3 = accumulator3;

							var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
							var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
							var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
							var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

							var result0 = Sse2.Add(product0, sum0);
							var result1 = Sse2.Add(product1, sum1);
							var result2 = Sse2.Add(product2, sum2);
							var result3 = Sse2.Add(product3, sum3);

							accumulator0 = result0;
							accumulator1 = result1;
							accumulator2 = result2;
							accumulator3 = result3;
						}
					}
				}

				{
					byte* lSecret = secret + (SecretLength - StripeLength);

					var accumulatorVec0 = accumulator0;
					var accumulatorVec1 = accumulator1;
					var accumulatorVec2 = accumulator2;
					var accumulatorVec3 = accumulator3;
					var shifted0 = Sse2.ShiftRightLogical(accumulatorVec0, 47);
					var shifted1 = Sse2.ShiftRightLogical(accumulatorVec1, 47);
					var shifted2 = Sse2.ShiftRightLogical(accumulatorVec2, 47);
					var shifted3 = Sse2.ShiftRightLogical(accumulatorVec3, 47);
					var dataVec0 = Sse2.Xor(accumulatorVec0, shifted0);
					var dataVec1 = Sse2.Xor(accumulatorVec1, shifted1);
					var dataVec2 = Sse2.Xor(accumulatorVec2, shifted2);
					var dataVec3 = Sse2.Xor(accumulatorVec3, shifted3);

					var keyVec0 = Sse2.LoadVector128(lSecret + 0x00u).AsUInt64();
					var keyVec1 = Sse2.LoadVector128(lSecret + 0x10u).AsUInt64();
					var keyVec2 = Sse2.LoadVector128(lSecret + 0x20u).AsUInt64();
					var keyVec3 = Sse2.LoadVector128(lSecret + 0x30u).AsUInt64();
					var dataKey0 = Sse2.Xor(dataVec0, keyVec0.AsUInt64());
					var dataKey1 = Sse2.Xor(dataVec1, keyVec1.AsUInt64());
					var dataKey2 = Sse2.Xor(dataVec2, keyVec2.AsUInt64());
					var dataKey3 = Sse2.Xor(dataVec3, keyVec3.AsUInt64());

					var dataKeyHi0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
					var dataKeyHi1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
					var dataKeyHi2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
					var dataKeyHi3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
					var productLo0 = Sse2.Multiply(dataKey0.AsUInt32(), primeVector);
					var productLo1 = Sse2.Multiply(dataKey1.AsUInt32(), primeVector);
					var productLo2 = Sse2.Multiply(dataKey2.AsUInt32(), primeVector);
					var productLo3 = Sse2.Multiply(dataKey3.AsUInt32(), primeVector);
					var productHi0 = Sse2.Multiply(dataKeyHi0.AsUInt32(), primeVector);
					var productHi1 = Sse2.Multiply(dataKeyHi1.AsUInt32(), primeVector);
					var productHi2 = Sse2.Multiply(dataKeyHi2.AsUInt32(), primeVector);
					var productHi3 = Sse2.Multiply(dataKeyHi3.AsUInt32(), primeVector);

					productHi0 = Sse2.ShiftLeftLogical(productHi0, 32);
					productHi1 = Sse2.ShiftLeftLogical(productHi1, 32);
					productHi2 = Sse2.ShiftLeftLogical(productHi2, 32);
					productHi3 = Sse2.ShiftLeftLogical(productHi3, 32);

					var sum0 = Sse2.Add(productLo0, productHi0);
					var sum1 = Sse2.Add(productLo1, productHi1);
					var sum2 = Sse2.Add(productLo2, productHi2);
					var sum3 = Sse2.Add(productLo3, productHi3);

					accumulator0 = sum0;
					accumulator1 = sum1;
					accumulator2 = sum2;
					accumulator3 = sum3;
				}
			}

			uint stripeCount = (length - 1u - (blockLength * blocks)) / StripeLength;
			{
				uint lDataOffset = blocks * blockLength;
				byte* lSecret = secret;
				uint stripe = 0;

				for (; stripe + 1 < stripeCount; stripe += 2) {
					{
						uint llDataOffset = lDataOffset + (stripe * StripeLength);
						byte* llSecret = lSecret + (stripe * 8u);

						span.SliceTo(localData, llDataOffset);
						byte* llData = localDataPtr;

						Vector128<ulong> result0, result1, result2, result3;

						{
							var dataVec0 = Sse2.LoadVector128(llData + 0x00u).AsUInt64();
							var dataVec1 = Sse2.LoadVector128(llData + 0x10u).AsUInt64();
							var dataVec2 = Sse2.LoadVector128(llData + 0x20u).AsUInt64();
							var dataVec3 = Sse2.LoadVector128(llData + 0x30u).AsUInt64();
							var keyVec0 = Sse2.LoadVector128(llSecret + 0x00u).AsUInt64();
							var keyVec1 = Sse2.LoadVector128(llSecret + 0x10u).AsUInt64();
							var keyVec2 = Sse2.LoadVector128(llSecret + 0x20u).AsUInt64();
							var keyVec3 = Sse2.LoadVector128(llSecret + 0x30u).AsUInt64();

							var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
							var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
							var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
							var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
							var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
							var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
							var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
							var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
							var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
							var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
							var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
							var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

							var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
							var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
							var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
							var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
							var addend0 = accumulator0;
							var addend1 = accumulator1;
							var addend2 = accumulator2;
							var addend3 = accumulator3;

							var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
							var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
							var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
							var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

							result0 = Sse2.Add(product0, sum0);
							result1 = Sse2.Add(product1, sum1);
							result2 = Sse2.Add(product2, sum2);
							result3 = Sse2.Add(product3, sum3);
						}

						{
							var dataVec0 = Sse2.LoadVector128(llData + 0x40u).AsUInt64();
							var dataVec1 = Sse2.LoadVector128(llData + 0x50u).AsUInt64();
							var dataVec2 = Sse2.LoadVector128(llData + 0x60u).AsUInt64();
							var dataVec3 = Sse2.LoadVector128(llData + 0x70u).AsUInt64();
							var keyVec0 = Sse2.LoadVector128(llSecret + 0x08u).AsUInt64();
							var keyVec1 = Sse2.LoadVector128(llSecret + 0x18u).AsUInt64();
							var keyVec2 = Sse2.LoadVector128(llSecret + 0x28u).AsUInt64();
							var keyVec3 = Sse2.LoadVector128(llSecret + 0x38u).AsUInt64();

							var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
							var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
							var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
							var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
							var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
							var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
							var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
							var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
							var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
							var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
							var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
							var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

							var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
							var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
							var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
							var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
							var addend0 = result0;
							var addend1 = result1;
							var addend2 = result2;
							var addend3 = result3;

							var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
							var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
							var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
							var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

							accumulator0 = Sse2.Add(product0, sum0);
							accumulator1 = Sse2.Add(product1, sum1);
							accumulator2 = Sse2.Add(product2, sum2);
							accumulator3 = Sse2.Add(product3, sum3);
						}
					}
				}

				if (stripe < stripeCount) {
					{
						uint llDataOffset = lDataOffset + (stripe * StripeLength);
						byte* llSecret = lSecret + (stripe * 8u);

						span.SliceTo(localData.Slice(0, 0x40), llDataOffset);
						byte* llData = localDataPtr;

						var dataVec0 = Sse2.LoadVector128(llData + 0x00u).AsUInt64();
						var dataVec1 = Sse2.LoadVector128(llData + 0x10u).AsUInt64();
						var dataVec2 = Sse2.LoadVector128(llData + 0x20u).AsUInt64();
						var dataVec3 = Sse2.LoadVector128(llData + 0x30u).AsUInt64();
						var keyVec0 = Sse2.LoadVector128(llSecret + 0x00u).AsUInt64();
						var keyVec1 = Sse2.LoadVector128(llSecret + 0x10u).AsUInt64();
						var keyVec2 = Sse2.LoadVector128(llSecret + 0x20u).AsUInt64();
						var keyVec3 = Sse2.LoadVector128(llSecret + 0x30u).AsUInt64();

						var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
						var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
						var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
						var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
						var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
						var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
						var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
						var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
						var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
						var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
						var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
						var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

						var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
						var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
						var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
						var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
						var addend0 = accumulator0;
						var addend1 = accumulator1;
						var addend2 = accumulator2;
						var addend3 = accumulator3;

						var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
						var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
						var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
						var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

						var result0 = Sse2.Add(product0, sum0);
						var result1 = Sse2.Add(product1, sum1);
						var result2 = Sse2.Add(product2, sum2);
						var result3 = Sse2.Add(product3, sum3);

						accumulator0 = result0;
						accumulator1 = result1;
						accumulator2 = result2;
						accumulator3 = result3;
					}
				}
			}

			{
				uint lDataOffset = length - StripeLength;
				byte* lSecret = secret + (SecretLength - StripeLength - 7u);

				span.SliceTo(localData.Slice(0, 0x40), lDataOffset);
				byte* lData = localDataPtr;

				var dataVec0 = Sse2.LoadVector128(lData + 0x00u).AsUInt64();
				var dataVec1 = Sse2.LoadVector128(lData + 0x10u).AsUInt64();
				var dataVec2 = Sse2.LoadVector128(lData + 0x20u).AsUInt64();
				var dataVec3 = Sse2.LoadVector128(lData + 0x30u).AsUInt64();
				var keyVec0 = Sse2.LoadVector128(lSecret + 0x00u).AsUInt64();
				var keyVec1 = Sse2.LoadVector128(lSecret + 0x10u).AsUInt64();
				var keyVec2 = Sse2.LoadVector128(lSecret + 0x20u).AsUInt64();
				var keyVec3 = Sse2.LoadVector128(lSecret + 0x30u).AsUInt64();

				var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
				var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
				var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
				var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
				var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
				var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
				var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
				var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
				var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
				var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
				var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
				var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

				var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
				var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
				var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
				var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
				var addend0 = accumulator0;
				var addend1 = accumulator1;
				var addend2 = accumulator2;
				var addend3 = accumulator3;

				var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
				var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
				var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
				var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

				var result0 = Sse2.Add(product0, sum0);
				var result1 = Sse2.Add(product1, sum1);
				var result2 = Sse2.Add(product2, sum2);
				var result3 = Sse2.Add(product3, sum3);

				accumulator0 = result0;
				accumulator1 = result1;
				accumulator2 = result2;
				accumulator3 = result3;
			}
		}

		ulong result = unchecked(length * Prime64.Prime0);

		var data0 = Sse2.Xor(accumulator0, Vector128.Create(SecretValues64.Secret0B, SecretValues64.Secret13));
		var data1 = Sse2.Xor(accumulator1, Vector128.Create(SecretValues64.Secret1B, SecretValues64.Secret23));
		var data2 = Sse2.Xor(accumulator2, Vector128.Create(SecretValues64.Secret2B, SecretValues64.Secret33));
		var data3 = Sse2.Xor(accumulator3, Vector128.Create(SecretValues64.Secret3B, SecretValues64.Secret43));

		result += MixAccumulators(data0.GetElement(0), data0.GetElement(1));
		result += MixAccumulators(data1.GetElement(0), data1.GetElement(1));
		result += MixAccumulators(data2.GetElement(0), data2.GetElement(1));
		result += MixAccumulators(data3.GetElement(0), data3.GetElement(1));

		return Avalanche(result);
	}

	[Pure]
	[MethodImpl(Inline)]
	private static ulong HashLongSse2PerBlock(SegmentedSpan span, uint length) {
		byte* secret = Secret;

		StripeLength.AssertEqual(64U);

		const uint stripesPerBlock = (SecretLength - StripeLength) / 8U;
		stripesPerBlock.AssertEqual(16u);
		const uint blockLength = StripeLength * stripesPerBlock;
		blockLength.AssertEqual(1024u);

		uint blocks = (length - 1) / blockLength;

		Vector128<ulong> accumulator0 = Vector128.Create(Prime32.Prime2, Prime64.Prime0);
		Vector128<ulong> accumulator1 = Vector128.Create(Prime64.Prime1, Prime64.Prime2);
		Vector128<ulong> accumulator2 = Vector128.Create(Prime64.Prime3, Prime32.Prime1);
		Vector128<ulong> accumulator3 = Vector128.Create(Prime64.Prime4, Prime32.Prime0);

		var primeVector = Vector128.Create(Prime32.Prime0);


		// Span rows are aligned per-block.
		uint rowsPerBlock = span.Width / blockLength;

		ReadOnlySpan<byte> GetBlock(SegmentedSpan span, uint block) {
			uint row = block / rowsPerBlock;
			uint offset = (block % rowsPerBlock) * blockLength;

			var rowSpan = span.Source.GetRowSpan((int)row);
			return rowSpan.Slice((int)offset, (int)blockLength);
		}

		uint block = 0;

		for (; block < blocks; ++block) {
			fixed (byte* lData = GetBlock(span, block)) {
				byte* lSecret = secret;

				uint stripe = 0;

				for (; stripe + 1 < stripesPerBlock; stripe += 2) {
					PrefetchNonTemporalNext(lData + (stripe * StripeLength) + 0x040);
					PrefetchNonTemporalNext(lData + (stripe * StripeLength) + 0x080);
					{
						byte* llData = lData + (stripe * StripeLength);
						byte* llSecret = lSecret + (stripe * 8u);

						Vector128<ulong> result0, result1, result2, result3;

						{
							var dataVec0 = Sse2.LoadVector128(llData + 0x00u).AsUInt64();
							var dataVec1 = Sse2.LoadVector128(llData + 0x10u).AsUInt64();
							var dataVec2 = Sse2.LoadVector128(llData + 0x20u).AsUInt64();
							var dataVec3 = Sse2.LoadVector128(llData + 0x30u).AsUInt64();
							var keyVec0 = Sse2.LoadVector128(llSecret + 0x00u).AsUInt64();
							var keyVec1 = Sse2.LoadVector128(llSecret + 0x10u).AsUInt64();
							var keyVec2 = Sse2.LoadVector128(llSecret + 0x20u).AsUInt64();
							var keyVec3 = Sse2.LoadVector128(llSecret + 0x30u).AsUInt64();

							var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
							var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
							var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
							var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
							var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
							var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
							var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
							var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
							var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
							var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
							var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
							var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

							var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
							var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
							var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
							var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
							var addend0 = accumulator0;
							var addend1 = accumulator1;
							var addend2 = accumulator2;
							var addend3 = accumulator3;

							var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
							var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
							var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
							var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

							result0 = Sse2.Add(product0, sum0);
							result1 = Sse2.Add(product1, sum1);
							result2 = Sse2.Add(product2, sum2);
							result3 = Sse2.Add(product3, sum3);
						}

						{
							var dataVec0 = Sse2.LoadVector128(llData + 0x40u).AsUInt64();
							var dataVec1 = Sse2.LoadVector128(llData + 0x50u).AsUInt64();
							var dataVec2 = Sse2.LoadVector128(llData + 0x60u).AsUInt64();
							var dataVec3 = Sse2.LoadVector128(llData + 0x70u).AsUInt64();
							var keyVec0 = Sse2.LoadVector128(llSecret + 0x08u).AsUInt64();
							var keyVec1 = Sse2.LoadVector128(llSecret + 0x18u).AsUInt64();
							var keyVec2 = Sse2.LoadVector128(llSecret + 0x28u).AsUInt64();
							var keyVec3 = Sse2.LoadVector128(llSecret + 0x38u).AsUInt64();

							var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
							var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
							var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
							var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
							var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
							var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
							var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
							var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
							var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
							var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
							var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
							var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

							var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
							var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
							var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
							var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
							var addend0 = result0;
							var addend1 = result1;
							var addend2 = result2;
							var addend3 = result3;

							var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
							var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
							var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
							var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

							accumulator0 = Sse2.Add(product0, sum0);
							accumulator1 = Sse2.Add(product1, sum1);
							accumulator2 = Sse2.Add(product2, sum2);
							accumulator3 = Sse2.Add(product3, sum3);
						}
					}
				}

				if (stripe < stripesPerBlock) {
					PrefetchNonTemporalNext(lData + (stripe * StripeLength) + 0x040);
					{
						byte* llData = lData + (stripe * StripeLength);
						byte* llSecret = lSecret + (stripe * 8u);

						var dataVec0 = Sse2.LoadVector128(llData + 0x00u).AsUInt64();
						var dataVec1 = Sse2.LoadVector128(llData + 0x10u).AsUInt64();
						var dataVec2 = Sse2.LoadVector128(llData + 0x20u).AsUInt64();
						var dataVec3 = Sse2.LoadVector128(llData + 0x30u).AsUInt64();
						var keyVec0 = Sse2.LoadVector128(llSecret + 0x00u).AsUInt64();
						var keyVec1 = Sse2.LoadVector128(llSecret + 0x10u).AsUInt64();
						var keyVec2 = Sse2.LoadVector128(llSecret + 0x20u).AsUInt64();
						var keyVec3 = Sse2.LoadVector128(llSecret + 0x30u).AsUInt64();

						var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
						var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
						var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
						var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
						var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
						var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
						var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
						var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
						var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
						var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
						var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
						var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

						var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
						var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
						var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
						var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
						var addend0 = accumulator0;
						var addend1 = accumulator1;
						var addend2 = accumulator2;
						var addend3 = accumulator3;

						var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
						var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
						var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
						var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

						var result0 = Sse2.Add(product0, sum0);
						var result1 = Sse2.Add(product1, sum1);
						var result2 = Sse2.Add(product2, sum2);
						var result3 = Sse2.Add(product3, sum3);

						accumulator0 = result0;
						accumulator1 = result1;
						accumulator2 = result2;
						accumulator3 = result3;
					}
				}
			}

			{
				byte* lSecret = secret + (SecretLength - StripeLength);

				var accumulatorVec0 = accumulator0;
				var accumulatorVec1 = accumulator1;
				var accumulatorVec2 = accumulator2;
				var accumulatorVec3 = accumulator3;
				var shifted0 = Sse2.ShiftRightLogical(accumulatorVec0, 47);
				var shifted1 = Sse2.ShiftRightLogical(accumulatorVec1, 47);
				var shifted2 = Sse2.ShiftRightLogical(accumulatorVec2, 47);
				var shifted3 = Sse2.ShiftRightLogical(accumulatorVec3, 47);
				var dataVec0 = Sse2.Xor(accumulatorVec0, shifted0);
				var dataVec1 = Sse2.Xor(accumulatorVec1, shifted1);
				var dataVec2 = Sse2.Xor(accumulatorVec2, shifted2);
				var dataVec3 = Sse2.Xor(accumulatorVec3, shifted3);

				var keyVec0 = Sse2.LoadVector128(lSecret + 0x00u).AsUInt64();
				var keyVec1 = Sse2.LoadVector128(lSecret + 0x10u).AsUInt64();
				var keyVec2 = Sse2.LoadVector128(lSecret + 0x20u).AsUInt64();
				var keyVec3 = Sse2.LoadVector128(lSecret + 0x30u).AsUInt64();
				var dataKey0 = Sse2.Xor(dataVec0, keyVec0.AsUInt64());
				var dataKey1 = Sse2.Xor(dataVec1, keyVec1.AsUInt64());
				var dataKey2 = Sse2.Xor(dataVec2, keyVec2.AsUInt64());
				var dataKey3 = Sse2.Xor(dataVec3, keyVec3.AsUInt64());

				var dataKeyHi0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
				var dataKeyHi1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
				var dataKeyHi2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
				var dataKeyHi3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
				var productLo0 = Sse2.Multiply(dataKey0.AsUInt32(), primeVector);
				var productLo1 = Sse2.Multiply(dataKey1.AsUInt32(), primeVector);
				var productLo2 = Sse2.Multiply(dataKey2.AsUInt32(), primeVector);
				var productLo3 = Sse2.Multiply(dataKey3.AsUInt32(), primeVector);
				var productHi0 = Sse2.Multiply(dataKeyHi0.AsUInt32(), primeVector);
				var productHi1 = Sse2.Multiply(dataKeyHi1.AsUInt32(), primeVector);
				var productHi2 = Sse2.Multiply(dataKeyHi2.AsUInt32(), primeVector);
				var productHi3 = Sse2.Multiply(dataKeyHi3.AsUInt32(), primeVector);

				productHi0 = Sse2.ShiftLeftLogical(productHi0, 32);
				productHi1 = Sse2.ShiftLeftLogical(productHi1, 32);
				productHi2 = Sse2.ShiftLeftLogical(productHi2, 32);
				productHi3 = Sse2.ShiftLeftLogical(productHi3, 32);

				var sum0 = Sse2.Add(productLo0, productHi0);
				var sum1 = Sse2.Add(productLo1, productHi1);
				var sum2 = Sse2.Add(productLo2, productHi2);
				var sum3 = Sse2.Add(productLo3, productHi3);

				accumulator0 = sum0;
				accumulator1 = sum1;
				accumulator2 = sum2;
				accumulator3 = sum3;
			}
		}

		uint stripeCount = (length - 1u - (blockLength * blocks)) / StripeLength;
		fixed (byte* lData = GetBlock(span, blocks)) {
			byte* lSecret = secret;
			uint stripe = 0;

			for (; stripe + 1 < stripeCount; stripe += 2) {
				PrefetchNonTemporalNext(lData + (stripe * StripeLength) + 0x040);
				PrefetchNonTemporalNext(lData + (stripe * StripeLength) + 0x080);
				{
					byte* llData = lData + (stripe * StripeLength);
					byte* llSecret = lSecret + (stripe * 8u);

					Vector128<ulong> result0, result1, result2, result3;

					{
						var dataVec0 = Sse2.LoadVector128(llData + 0x00u).AsUInt64();
						var dataVec1 = Sse2.LoadVector128(llData + 0x10u).AsUInt64();
						var dataVec2 = Sse2.LoadVector128(llData + 0x20u).AsUInt64();
						var dataVec3 = Sse2.LoadVector128(llData + 0x30u).AsUInt64();
						var keyVec0 = Sse2.LoadVector128(llSecret + 0x00u).AsUInt64();
						var keyVec1 = Sse2.LoadVector128(llSecret + 0x10u).AsUInt64();
						var keyVec2 = Sse2.LoadVector128(llSecret + 0x20u).AsUInt64();
						var keyVec3 = Sse2.LoadVector128(llSecret + 0x30u).AsUInt64();

						var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
						var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
						var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
						var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
						var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
						var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
						var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
						var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
						var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
						var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
						var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
						var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

						var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
						var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
						var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
						var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
						var addend0 = accumulator0;
						var addend1 = accumulator1;
						var addend2 = accumulator2;
						var addend3 = accumulator3;

						var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
						var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
						var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
						var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

						result0 = Sse2.Add(product0, sum0);
						result1 = Sse2.Add(product1, sum1);
						result2 = Sse2.Add(product2, sum2);
						result3 = Sse2.Add(product3, sum3);
					}

					{
						var dataVec0 = Sse2.LoadVector128(llData + 0x40u).AsUInt64();
						var dataVec1 = Sse2.LoadVector128(llData + 0x50u).AsUInt64();
						var dataVec2 = Sse2.LoadVector128(llData + 0x60u).AsUInt64();
						var dataVec3 = Sse2.LoadVector128(llData + 0x70u).AsUInt64();
						var keyVec0 = Sse2.LoadVector128(llSecret + 0x08u).AsUInt64();
						var keyVec1 = Sse2.LoadVector128(llSecret + 0x18u).AsUInt64();
						var keyVec2 = Sse2.LoadVector128(llSecret + 0x28u).AsUInt64();
						var keyVec3 = Sse2.LoadVector128(llSecret + 0x38u).AsUInt64();

						var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
						var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
						var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
						var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
						var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
						var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
						var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
						var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
						var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
						var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
						var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
						var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

						var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
						var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
						var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
						var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
						var addend0 = result0;
						var addend1 = result1;
						var addend2 = result2;
						var addend3 = result3;

						var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
						var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
						var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
						var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

						accumulator0 = Sse2.Add(product0, sum0);
						accumulator1 = Sse2.Add(product1, sum1);
						accumulator2 = Sse2.Add(product2, sum2);
						accumulator3 = Sse2.Add(product3, sum3);
					}
				}
			}

			if (stripe < stripeCount) {
				PrefetchNonTemporalNext(lData + (stripe * StripeLength) + 0x040);
				{
					byte* llData = lData + (stripe * StripeLength);
					byte* llSecret = lSecret + (stripe * 8u);

					var dataVec0 = Sse2.LoadVector128(llData + 0x00u).AsUInt64();
					var dataVec1 = Sse2.LoadVector128(llData + 0x10u).AsUInt64();
					var dataVec2 = Sse2.LoadVector128(llData + 0x20u).AsUInt64();
					var dataVec3 = Sse2.LoadVector128(llData + 0x30u).AsUInt64();
					var keyVec0 = Sse2.LoadVector128(llSecret + 0x00u).AsUInt64();
					var keyVec1 = Sse2.LoadVector128(llSecret + 0x10u).AsUInt64();
					var keyVec2 = Sse2.LoadVector128(llSecret + 0x20u).AsUInt64();
					var keyVec3 = Sse2.LoadVector128(llSecret + 0x30u).AsUInt64();

					var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
					var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
					var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
					var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
					var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
					var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
					var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
					var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
					var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
					var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
					var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
					var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

					var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
					var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
					var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
					var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
					var addend0 = accumulator0;
					var addend1 = accumulator1;
					var addend2 = accumulator2;
					var addend3 = accumulator3;

					var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
					var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
					var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
					var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

					var result0 = Sse2.Add(product0, sum0);
					var result1 = Sse2.Add(product1, sum1);
					var result2 = Sse2.Add(product2, sum2);
					var result3 = Sse2.Add(product3, sum3);

					accumulator0 = result0;
					accumulator1 = result1;
					accumulator2 = result2;
					accumulator3 = result3;
				}
			}
		}

		Span<byte> localData = stackalloc byte[64];
		fixed (byte* lData = span.SliceTo(localData, length - StripeLength)) {
			byte* lSecret = secret + (SecretLength - StripeLength - 7u);

			var dataVec0 = Sse2.LoadVector128(lData + 0x00u).AsUInt64();
			var dataVec1 = Sse2.LoadVector128(lData + 0x10u).AsUInt64();
			var dataVec2 = Sse2.LoadVector128(lData + 0x20u).AsUInt64();
			var dataVec3 = Sse2.LoadVector128(lData + 0x30u).AsUInt64();
			var keyVec0 = Sse2.LoadVector128(lSecret + 0x00u).AsUInt64();
			var keyVec1 = Sse2.LoadVector128(lSecret + 0x10u).AsUInt64();
			var keyVec2 = Sse2.LoadVector128(lSecret + 0x20u).AsUInt64();
			var keyVec3 = Sse2.LoadVector128(lSecret + 0x30u).AsUInt64();

			var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
			var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
			var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
			var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
			var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
			var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
			var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
			var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
			var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
			var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
			var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
			var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

			var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
			var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
			var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
			var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
			var addend0 = accumulator0;
			var addend1 = accumulator1;
			var addend2 = accumulator2;
			var addend3 = accumulator3;

			var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
			var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
			var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
			var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

			var result0 = Sse2.Add(product0, sum0);
			var result1 = Sse2.Add(product1, sum1);
			var result2 = Sse2.Add(product2, sum2);
			var result3 = Sse2.Add(product3, sum3);

			accumulator0 = result0;
			accumulator1 = result1;
			accumulator2 = result2;
			accumulator3 = result3;
		}

		ulong result = unchecked(length * Prime64.Prime0);

		var data0 = Sse2.Xor(accumulator0, Vector128.Create(SecretValues64.Secret0B, SecretValues64.Secret13));
		var data1 = Sse2.Xor(accumulator1, Vector128.Create(SecretValues64.Secret1B, SecretValues64.Secret23));
		var data2 = Sse2.Xor(accumulator2, Vector128.Create(SecretValues64.Secret2B, SecretValues64.Secret33));
		var data3 = Sse2.Xor(accumulator3, Vector128.Create(SecretValues64.Secret3B, SecretValues64.Secret43));

		result += MixAccumulators(data0.GetElement(0), data0.GetElement(1));
		result += MixAccumulators(data1.GetElement(0), data1.GetElement(1));
		result += MixAccumulators(data2.GetElement(0), data2.GetElement(1));
		result += MixAccumulators(data3.GetElement(0), data3.GetElement(1));

		return Avalanche(result);
	}

	[Pure]
	[MethodImpl(Inline)]
	private static ulong HashLongSse2PerStripe(SegmentedSpan span, uint length) {
		byte* secret = Secret;

		StripeLength.AssertEqual(64U);

		const uint stripesPerBlock = (SecretLength - StripeLength) / 8U;
		stripesPerBlock.AssertEqual(16u);
		const uint blockLength = StripeLength * stripesPerBlock;
		blockLength.AssertEqual(1024u);

		uint blocks = (length - 1) / blockLength;

		Vector128<ulong> accumulator0 = Vector128.Create(Prime32.Prime2, Prime64.Prime0);
		Vector128<ulong> accumulator1 = Vector128.Create(Prime64.Prime1, Prime64.Prime2);
		Vector128<ulong> accumulator2 = Vector128.Create(Prime64.Prime3, Prime32.Prime1);
		Vector128<ulong> accumulator3 = Vector128.Create(Prime64.Prime4, Prime32.Prime0);

		var primeVector = Vector128.Create(Prime32.Prime0);

		// Span rows are aligned per-stripe.
		uint rowsPerStripe = span.Width / StripeLength;

		[MethodImpl(Inline)]
		ReadOnlySpan<byte> GetStripe(SegmentedSpan span, uint offset, uint stripe, uint stripeCount = 1) {
			uint offsetStripe = offset / StripeLength;
			uint relativeStripe = stripe + offsetStripe;
			uint row = relativeStripe / rowsPerStripe;
			uint spanOffset = (relativeStripe % rowsPerStripe) * StripeLength;

			var rowSpan = span.Source.GetRowSpan((int)row);
			var result = rowSpan.Slice((int)spanOffset, (int)(StripeLength * stripeCount));

			return result;
		}

		uint block = 0;

		for (; block < blocks; ++block) {
			{
				uint lDataOffset = block * blockLength;
				byte* lSecret = secret;

				uint stripe = 0;

				for (; stripe < stripesPerBlock; ++stripe) {
					fixed (byte* llData = GetStripe(span, lDataOffset, stripe)) {
						byte* llSecret = lSecret + (stripe * 8u);

						var dataVec0 = Sse2.LoadVector128(llData + 0x00u).AsUInt64();
						var dataVec1 = Sse2.LoadVector128(llData + 0x10u).AsUInt64();
						var dataVec2 = Sse2.LoadVector128(llData + 0x20u).AsUInt64();
						var dataVec3 = Sse2.LoadVector128(llData + 0x30u).AsUInt64();
						var keyVec0 = Sse2.LoadVector128(llSecret + 0x00u).AsUInt64();
						var keyVec1 = Sse2.LoadVector128(llSecret + 0x10u).AsUInt64();
						var keyVec2 = Sse2.LoadVector128(llSecret + 0x20u).AsUInt64();
						var keyVec3 = Sse2.LoadVector128(llSecret + 0x30u).AsUInt64();

						var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
						var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
						var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
						var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
						var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
						var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
						var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
						var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
						var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
						var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
						var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
						var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

						var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
						var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
						var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
						var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
						var addend0 = accumulator0;
						var addend1 = accumulator1;
						var addend2 = accumulator2;
						var addend3 = accumulator3;

						var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
						var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
						var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
						var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

						accumulator0 = Sse2.Add(product0, sum0);
						accumulator1 = Sse2.Add(product1, sum1);
						accumulator2 = Sse2.Add(product2, sum2);
						accumulator3 = Sse2.Add(product3, sum3);
					}
				}
			}

			{
				byte* lSecret = secret + (SecretLength - StripeLength);

				var accumulatorVec0 = accumulator0;
				var accumulatorVec1 = accumulator1;
				var accumulatorVec2 = accumulator2;
				var accumulatorVec3 = accumulator3;
				var shifted0 = Sse2.ShiftRightLogical(accumulatorVec0, 47);
				var shifted1 = Sse2.ShiftRightLogical(accumulatorVec1, 47);
				var shifted2 = Sse2.ShiftRightLogical(accumulatorVec2, 47);
				var shifted3 = Sse2.ShiftRightLogical(accumulatorVec3, 47);
				var dataVec0 = Sse2.Xor(accumulatorVec0, shifted0);
				var dataVec1 = Sse2.Xor(accumulatorVec1, shifted1);
				var dataVec2 = Sse2.Xor(accumulatorVec2, shifted2);
				var dataVec3 = Sse2.Xor(accumulatorVec3, shifted3);

				var keyVec0 = Sse2.LoadVector128(lSecret + 0x00u).AsUInt64();
				var keyVec1 = Sse2.LoadVector128(lSecret + 0x10u).AsUInt64();
				var keyVec2 = Sse2.LoadVector128(lSecret + 0x20u).AsUInt64();
				var keyVec3 = Sse2.LoadVector128(lSecret + 0x30u).AsUInt64();
				var dataKey0 = Sse2.Xor(dataVec0, keyVec0.AsUInt64());
				var dataKey1 = Sse2.Xor(dataVec1, keyVec1.AsUInt64());
				var dataKey2 = Sse2.Xor(dataVec2, keyVec2.AsUInt64());
				var dataKey3 = Sse2.Xor(dataVec3, keyVec3.AsUInt64());

				var dataKeyHi0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
				var dataKeyHi1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
				var dataKeyHi2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
				var dataKeyHi3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
				var productLo0 = Sse2.Multiply(dataKey0.AsUInt32(), primeVector);
				var productLo1 = Sse2.Multiply(dataKey1.AsUInt32(), primeVector);
				var productLo2 = Sse2.Multiply(dataKey2.AsUInt32(), primeVector);
				var productLo3 = Sse2.Multiply(dataKey3.AsUInt32(), primeVector);
				var productHi0 = Sse2.Multiply(dataKeyHi0.AsUInt32(), primeVector);
				var productHi1 = Sse2.Multiply(dataKeyHi1.AsUInt32(), primeVector);
				var productHi2 = Sse2.Multiply(dataKeyHi2.AsUInt32(), primeVector);
				var productHi3 = Sse2.Multiply(dataKeyHi3.AsUInt32(), primeVector);

				productHi0 = Sse2.ShiftLeftLogical(productHi0, 32);
				productHi1 = Sse2.ShiftLeftLogical(productHi1, 32);
				productHi2 = Sse2.ShiftLeftLogical(productHi2, 32);
				productHi3 = Sse2.ShiftLeftLogical(productHi3, 32);

				var sum0 = Sse2.Add(productLo0, productHi0);
				var sum1 = Sse2.Add(productLo1, productHi1);
				var sum2 = Sse2.Add(productLo2, productHi2);
				var sum3 = Sse2.Add(productLo3, productHi3);

				accumulator0 = sum0;
				accumulator1 = sum1;
				accumulator2 = sum2;
				accumulator3 = sum3;
			}
		}

		uint stripeCount = (length - 1u - (blockLength * blocks)) / StripeLength;
		{
			uint lDataOffset = blocks * blockLength;
			byte* lSecret = secret;
			uint stripe = 0;

			for (; stripe < stripeCount; ++stripe) {
				fixed (byte* llData = GetStripe(span, lDataOffset, stripe)) {
					byte* llSecret = lSecret + (stripe * 8u);

					var dataVec0 = Sse2.LoadVector128(llData + 0x00u).AsUInt64();
					var dataVec1 = Sse2.LoadVector128(llData + 0x10u).AsUInt64();
					var dataVec2 = Sse2.LoadVector128(llData + 0x20u).AsUInt64();
					var dataVec3 = Sse2.LoadVector128(llData + 0x30u).AsUInt64();
					var keyVec0 = Sse2.LoadVector128(llSecret + 0x00u).AsUInt64();
					var keyVec1 = Sse2.LoadVector128(llSecret + 0x10u).AsUInt64();
					var keyVec2 = Sse2.LoadVector128(llSecret + 0x20u).AsUInt64();
					var keyVec3 = Sse2.LoadVector128(llSecret + 0x30u).AsUInt64();

					var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
					var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
					var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
					var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
					var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
					var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
					var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
					var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
					var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
					var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
					var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
					var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

					var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
					var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
					var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
					var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
					var addend0 = accumulator0;
					var addend1 = accumulator1;
					var addend2 = accumulator2;
					var addend3 = accumulator3;

					var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
					var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
					var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
					var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

					accumulator0 = Sse2.Add(product0, sum0);
					accumulator1 = Sse2.Add(product1, sum1);
					accumulator2 = Sse2.Add(product2, sum2);
					accumulator3 = Sse2.Add(product3, sum3);
				}
			}
		}

		Span<byte> localData = stackalloc byte[64];

		fixed (byte* lData = span.SliceTo(localData, length - StripeLength)) {
			byte* lSecret = secret + (SecretLength - StripeLength - 7u);

			var dataVec0 = Sse2.LoadVector128(lData + 0x00u).AsUInt64();
			var dataVec1 = Sse2.LoadVector128(lData + 0x10u).AsUInt64();
			var dataVec2 = Sse2.LoadVector128(lData + 0x20u).AsUInt64();
			var dataVec3 = Sse2.LoadVector128(lData + 0x30u).AsUInt64();
			var keyVec0 = Sse2.LoadVector128(lSecret + 0x00u).AsUInt64();
			var keyVec1 = Sse2.LoadVector128(lSecret + 0x10u).AsUInt64();
			var keyVec2 = Sse2.LoadVector128(lSecret + 0x20u).AsUInt64();
			var keyVec3 = Sse2.LoadVector128(lSecret + 0x30u).AsUInt64();

			var dataKey0 = Sse2.Xor(dataVec0, keyVec0);
			var dataKey1 = Sse2.Xor(dataVec1, keyVec1);
			var dataKey2 = Sse2.Xor(dataVec2, keyVec2);
			var dataKey3 = Sse2.Xor(dataVec3, keyVec3);
			var dataKeyLo0 = Sse2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
			var dataKeyLo1 = Sse2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
			var dataKeyLo2 = Sse2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
			var dataKeyLo3 = Sse2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
			var product0 = Sse2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
			var product1 = Sse2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);
			var product2 = Sse2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
			var product3 = Sse2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

			var dataSwap0 = Sse2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
			var dataSwap1 = Sse2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
			var dataSwap2 = Sse2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
			var dataSwap3 = Sse2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
			var addend0 = accumulator0;
			var addend1 = accumulator1;
			var addend2 = accumulator2;
			var addend3 = accumulator3;

			var sum0 = Sse2.Add(addend0, dataSwap0.AsUInt64());
			var sum1 = Sse2.Add(addend1, dataSwap1.AsUInt64());
			var sum2 = Sse2.Add(addend2, dataSwap2.AsUInt64());
			var sum3 = Sse2.Add(addend3, dataSwap3.AsUInt64());

			var result0 = Sse2.Add(product0, sum0);
			var result1 = Sse2.Add(product1, sum1);
			var result2 = Sse2.Add(product2, sum2);
			var result3 = Sse2.Add(product3, sum3);

			accumulator0 = result0;
			accumulator1 = result1;
			accumulator2 = result2;
			accumulator3 = result3;
		}

		ulong result = unchecked(length * Prime64.Prime0);

		var data0 = Sse2.Xor(accumulator0, Vector128.Create(SecretValues64.Secret0B, SecretValues64.Secret13));
		var data1 = Sse2.Xor(accumulator1, Vector128.Create(SecretValues64.Secret1B, SecretValues64.Secret23));
		var data2 = Sse2.Xor(accumulator2, Vector128.Create(SecretValues64.Secret2B, SecretValues64.Secret33));
		var data3 = Sse2.Xor(accumulator3, Vector128.Create(SecretValues64.Secret3B, SecretValues64.Secret43));

		result += MixAccumulators(data0.GetElement(0), data0.GetElement(1));
		result += MixAccumulators(data1.GetElement(0), data1.GetElement(1));
		result += MixAccumulators(data2.GetElement(0), data2.GetElement(1));
		result += MixAccumulators(data3.GetElement(0), data3.GetElement(1));

		return Avalanche(result);
	}
}
