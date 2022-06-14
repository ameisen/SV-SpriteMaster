using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SpriteMaster.Hashing.Algorithms;

internal static unsafe partial class XxHash3 {
	// xxh3_accumulate_512_avx2
	[MethodImpl(Inline)]
	private static void Accumulate512Avx2(ulong* accumulatorStore, byte* data, byte* secret) {
		//PrefetchNonTemporalNext(data);
		//PrefetchNonTemporalNext(secret);

		ref var accumulator = ref *(CombinedVector256X512<ulong>*)accumulatorStore;

		var dataVec0 = *(Vector256<ulong>*)(data + 0x00u);
		var dataVec1 = *(Vector256<ulong>*)(data + 0x20u);
		var keyVec0 = *(Vector256<ulong>*)(secret + 0x00u);
		var keyVec1 = *(Vector256<ulong>*)(secret + 0x20u);

		var dataKey0 = Avx2.Xor(dataVec0, keyVec0);
		var dataKey1 = Avx2.Xor(dataVec1, keyVec1);
		var dataKeyLo0 = Avx2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
		var dataKeyLo1 = Avx2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
		var product0 = Avx2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
		var product1 = Avx2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);

		var dataSwap0 = Avx2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
		var dataSwap1 = Avx2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
		var addend0 = accumulator.Data0;
		var addend1 = accumulator.Data1;
		var sum0 = Avx2.Add(addend0, dataSwap0.AsUInt64());
		var sum1 = Avx2.Add(addend1, dataSwap1.AsUInt64());

		var result0 = Avx2.Add(product0, sum0);
		var result1 = Avx2.Add(product1, sum1);

		accumulator.Data0 = result0;
		accumulator.Data1 = result1;
	}

	[MethodImpl(Inline)]
	private static void Accumulate1024Avx2(ulong* accumulatorStore, byte* data, byte* secret) {
		//PrefetchNonTemporalNext(data);
		//PrefetchNonTemporalNext(data + 0x40);

		ref var accumulator = ref *(CombinedVector256X512<ulong>*)accumulatorStore;

		var dataVec0 = *(Vector256<ulong>*)(data + 0x00u);
		var dataVec1 = *(Vector256<ulong>*)(data + 0x20u);
		var keyVec0 = *(Vector256<ulong>*)(secret + 0x00u);
		var keyVec1 = *(Vector256<ulong>*)(secret + 0x20u);

		var dataKey0 = Avx2.Xor(dataVec0, keyVec0);
		var dataKey1 = Avx2.Xor(dataVec1, keyVec1);
		var dataKeyLo0 = Avx2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
		var dataKeyLo1 = Avx2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
		var product0 = Avx2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
		var product1 = Avx2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);

		var dataSwap0 = Avx2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
		var dataSwap1 = Avx2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
		var addend0 = accumulator.Data0;
		var addend1 = accumulator.Data1;
		var sum0 = Avx2.Add(addend0, dataSwap0.AsUInt64());
		var sum1 = Avx2.Add(addend1, dataSwap1.AsUInt64());

		var result0 = Avx2.Add(product0, sum0);
		var result1 = Avx2.Add(product1, sum1);

		addend0 = result0;
		addend1 = result1;

		var dataVec2 = *(Vector256<ulong>*)(data + 0x40u);
		var dataVec3 = *(Vector256<ulong>*)(data + 0x60u);
		var keyVec2 = *(Vector256<ulong>*)(secret + 0x08u);
		var keyVec3 = *(Vector256<ulong>*)(secret + 0x28u);

		var dataKey2 = Avx2.Xor(dataVec2, keyVec2);
		var dataKey3 = Avx2.Xor(dataVec3, keyVec3);
		var dataKeyLo2 = Avx2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
		var dataKeyLo3 = Avx2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
		var product2 = Avx2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
		var product3 = Avx2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

		var dataSwap2 = Avx2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
		var dataSwap3 = Avx2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
		var sum2 = Avx2.Add(addend0, dataSwap2.AsUInt64());
		var sum3 = Avx2.Add(addend1, dataSwap3.AsUInt64());

		var result2 = Avx2.Add(product2, sum2);
		var result3 = Avx2.Add(product3, sum3);

		accumulator.Data0 = result2;
		accumulator.Data1 = result3;
	}

	// xxh3_scramble_acc_avx2
	[MethodImpl(Inline)]
	private static void ScrambleAccumulatorAvx2(ulong* accumulatorStore, byte* secret) {
		ref var accumulator = ref *(CombinedVector256X512<ulong>*)accumulatorStore;

		var primeVector = Vector256.Create(Prime32.Prime0);

		var accumulatorVec0 = accumulator.Data0;
		var accumulatorVec1 = accumulator.Data1;
		var shifted0 = Avx2.ShiftRightLogical(accumulatorVec0, 47);
		var shifted1 = Avx2.ShiftRightLogical(accumulatorVec1, 47);
		var dataVec0 = Avx2.Xor(accumulatorVec0, shifted0);
		var dataVec1 = Avx2.Xor(accumulatorVec1, shifted1);

		var keyVec0 = Avx2.LoadVector256(secret + 0x00u).AsUInt64();
		var keyVec1 = Avx2.LoadVector256(secret + 0x20u).AsUInt64();
		var dataKey0 = Avx2.Xor(dataVec0, keyVec0.AsUInt64());
		var dataKey1 = Avx2.Xor(dataVec1, keyVec1.AsUInt64());

		var dataKeyHi0 = Avx2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
		var dataKeyHi1 = Avx2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
		var productLo0 = Avx2.Multiply(dataKey0.AsUInt32(), primeVector);
		var productLo1 = Avx2.Multiply(dataKey1.AsUInt32(), primeVector);
		var productHi0 = Avx2.Multiply(dataKeyHi0.AsUInt32(), primeVector);
		var productHi1 = Avx2.Multiply(dataKeyHi1.AsUInt32(), primeVector);

		productHi0 = Avx2.ShiftLeftLogical(productHi0, 32);
		productHi1 = Avx2.ShiftLeftLogical(productHi1, 32);

		var sum0 = Avx2.Add(productLo0, productHi0);
		var sum1 = Avx2.Add(productLo1, productHi1);

		accumulator.Data0 = sum0;
		accumulator.Data1 = sum1;
	}

	[Pure]
	[MethodImpl(Inline)]
	private static ulong HashLongAvx2(byte* data, uint length) {
		byte* secret = Secret;

		StripeLength.AssertEqual(64U);

		const uint stripesPerBlock = (SecretLength - StripeLength) / 8U;
		stripesPerBlock.AssertEqual(16u);
		const uint blockLength = StripeLength * stripesPerBlock;
		blockLength.AssertEqual(1024u);

		uint blocks = (length - 1) / blockLength;

		Vector256<ulong> accumulator0 = Vector256.Create(Prime32.Prime2, Prime64.Prime0, Prime64.Prime1, Prime64.Prime2);
		Vector256<ulong> accumulator1 = Vector256.Create(Prime64.Prime3, Prime32.Prime1, Prime64.Prime4, Prime32.Prime0);

		var primeVector = Vector256.Create(Prime32.Prime0);

		for (uint block = 0; block < blocks; ++block) {
			{
				byte* lData = data + (block * blockLength);
				byte* lSecret = secret;
				uint stripe = 0;

				for (; stripe + 1 < stripesPerBlock; stripe += 2) {
					PrefetchNonTemporalNext(lData + (stripe * StripeLength) + 0x040);
					PrefetchNonTemporalNext(lData + (stripe * StripeLength) + 0x080);
					{
						byte* llData = lData + (stripe * StripeLength);
						byte* llSecret = lSecret + (stripe * 8u);

						var dataVec0 = *(Vector256<ulong>*)(llData + 0x00u);
						var dataVec1 = *(Vector256<ulong>*)(llData + 0x20u);
						var keyVec0 = *(Vector256<ulong>*)(llSecret + 0x00u);
						var keyVec1 = *(Vector256<ulong>*)(llSecret + 0x20u);

						var dataKey0 = Avx2.Xor(dataVec0, keyVec0);
						var dataKey1 = Avx2.Xor(dataVec1, keyVec1);
						var dataKeyLo0 = Avx2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
						var dataKeyLo1 = Avx2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
						var product0 = Avx2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
						var product1 = Avx2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);

						var dataSwap0 = Avx2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
						var dataSwap1 = Avx2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
						var addend0 = accumulator0;
						var addend1 = accumulator1;
						var sum0 = Avx2.Add(addend0, dataSwap0.AsUInt64());
						var sum1 = Avx2.Add(addend1, dataSwap1.AsUInt64());

						var result0 = Avx2.Add(product0, sum0);
						var result1 = Avx2.Add(product1, sum1);

						addend0 = result0;
						addend1 = result1;

						var dataVec2 = *(Vector256<ulong>*)(llData + 0x40u);
						var dataVec3 = *(Vector256<ulong>*)(llData + 0x60u);
						var keyVec2 = *(Vector256<ulong>*)(llSecret + 0x08u);
						var keyVec3 = *(Vector256<ulong>*)(llSecret + 0x28u);

						var dataKey2 = Avx2.Xor(dataVec2, keyVec2);
						var dataKey3 = Avx2.Xor(dataVec3, keyVec3);
						var dataKeyLo2 = Avx2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
						var dataKeyLo3 = Avx2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
						var product2 = Avx2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
						var product3 = Avx2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

						var dataSwap2 = Avx2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
						var dataSwap3 = Avx2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
						var sum2 = Avx2.Add(addend0, dataSwap2.AsUInt64());
						var sum3 = Avx2.Add(addend1, dataSwap3.AsUInt64());

						var result2 = Avx2.Add(product2, sum2);
						var result3 = Avx2.Add(product3, sum3);

						accumulator0 = result2;
						accumulator1 = result3;
					}
				}

				if (stripe < stripesPerBlock) {
					PrefetchNonTemporalNext(lData + (stripe * StripeLength) + 0x040);
					{
						byte* llData = lData + (stripe * StripeLength);
						byte* llSecret = lSecret + (stripe * 8u);

						var dataVec0 = *(Vector256<ulong>*)(llData + 0x00u);
						var dataVec1 = *(Vector256<ulong>*)(llData + 0x20u);
						var keyVec0 = *(Vector256<ulong>*)(llSecret + 0x00u);
						var keyVec1 = *(Vector256<ulong>*)(llSecret + 0x20u);

						var dataKey0 = Avx2.Xor(dataVec0, keyVec0);
						var dataKey1 = Avx2.Xor(dataVec1, keyVec1);
						var dataKeyLo0 = Avx2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
						var dataKeyLo1 = Avx2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
						var product0 = Avx2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
						var product1 = Avx2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);

						var dataSwap0 = Avx2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
						var dataSwap1 = Avx2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
						var addend0 = accumulator0;
						var addend1 = accumulator1;
						var sum0 = Avx2.Add(addend0, dataSwap0.AsUInt64());
						var sum1 = Avx2.Add(addend1, dataSwap1.AsUInt64());

						var result0 = Avx2.Add(product0, sum0);
						var result1 = Avx2.Add(product1, sum1);

						accumulator0 = result0;
						accumulator1 = result1;
					}
				}
			}

			{
				byte* lSecret = secret + (SecretLength - StripeLength);

				var accumulatorVec0 = accumulator0;
				var accumulatorVec1 = accumulator1;
				var shifted0 = Avx2.ShiftRightLogical(accumulatorVec0, 47);
				var shifted1 = Avx2.ShiftRightLogical(accumulatorVec1, 47);
				var dataVec0 = Avx2.Xor(accumulatorVec0, shifted0);
				var dataVec1 = Avx2.Xor(accumulatorVec1, shifted1);

				var keyVec0 = Avx2.LoadVector256(lSecret + 0x00u).AsUInt64();
				var keyVec1 = Avx2.LoadVector256(lSecret + 0x20u).AsUInt64();
				var dataKey0 = Avx2.Xor(dataVec0, keyVec0.AsUInt64());
				var dataKey1 = Avx2.Xor(dataVec1, keyVec1.AsUInt64());

				var dataKeyHi0 = Avx2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
				var dataKeyHi1 = Avx2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
				var productLo0 = Avx2.Multiply(dataKey0.AsUInt32(), primeVector);
				var productLo1 = Avx2.Multiply(dataKey1.AsUInt32(), primeVector);
				var productHi0 = Avx2.Multiply(dataKeyHi0.AsUInt32(), primeVector);
				var productHi1 = Avx2.Multiply(dataKeyHi1.AsUInt32(), primeVector);

				productHi0 = Avx2.ShiftLeftLogical(productHi0, 32);
				productHi1 = Avx2.ShiftLeftLogical(productHi1, 32);

				var sum0 = Avx2.Add(productLo0, productHi0);
				var sum1 = Avx2.Add(productLo1, productHi1);

				accumulator0 = sum0;
				accumulator1 = sum1;
			}
		}

		uint stripeCount = (length - 1u - (blockLength * blocks)) / StripeLength;
		{
			byte* lData = data + (blocks * blockLength);
			byte* lSecret = secret;

			uint stripe = 0;

			for (; stripe + 1 < stripeCount; stripe += 2) {
				PrefetchNonTemporalNext(lData + (stripe * StripeLength) + 0x040);
				PrefetchNonTemporalNext(lData + (stripe * StripeLength) + 0x080);
				{
					byte* llData = lData + (stripe * StripeLength);
					byte* llSecret = lSecret + (stripe * 8u);

					var dataVec0 = *(Vector256<ulong>*)(llData + 0x00u);
					var dataVec1 = *(Vector256<ulong>*)(llData + 0x20u);
					var keyVec0 = *(Vector256<ulong>*)(llSecret + 0x00u);
					var keyVec1 = *(Vector256<ulong>*)(llSecret + 0x20u);

					var dataKey0 = Avx2.Xor(dataVec0, keyVec0);
					var dataKey1 = Avx2.Xor(dataVec1, keyVec1);
					var dataKeyLo0 = Avx2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
					var dataKeyLo1 = Avx2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
					var product0 = Avx2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
					var product1 = Avx2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);

					var dataSwap0 = Avx2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
					var dataSwap1 = Avx2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
					var addend0 = accumulator0;
					var addend1 = accumulator1;
					var sum0 = Avx2.Add(addend0, dataSwap0.AsUInt64());
					var sum1 = Avx2.Add(addend1, dataSwap1.AsUInt64());

					var result0 = Avx2.Add(product0, sum0);
					var result1 = Avx2.Add(product1, sum1);

					addend0 = result0;
					addend1 = result1;

					var dataVec2 = *(Vector256<ulong>*)(llData + 0x40u);
					var dataVec3 = *(Vector256<ulong>*)(llData + 0x60u);
					var keyVec2 = *(Vector256<ulong>*)(llSecret + 0x08u);
					var keyVec3 = *(Vector256<ulong>*)(llSecret + 0x28u);

					var dataKey2 = Avx2.Xor(dataVec2, keyVec2);
					var dataKey3 = Avx2.Xor(dataVec3, keyVec3);
					var dataKeyLo2 = Avx2.Shuffle(dataKey2.AsUInt32(), ShuffleDataKey);
					var dataKeyLo3 = Avx2.Shuffle(dataKey3.AsUInt32(), ShuffleDataKey);
					var product2 = Avx2.Multiply(dataKey2.AsUInt32(), dataKeyLo2);
					var product3 = Avx2.Multiply(dataKey3.AsUInt32(), dataKeyLo3);

					var dataSwap2 = Avx2.Shuffle(dataVec2.AsUInt32(), ShuffleDataSwap);
					var dataSwap3 = Avx2.Shuffle(dataVec3.AsUInt32(), ShuffleDataSwap);
					var sum2 = Avx2.Add(addend0, dataSwap2.AsUInt64());
					var sum3 = Avx2.Add(addend1, dataSwap3.AsUInt64());

					var result2 = Avx2.Add(product2, sum2);
					var result3 = Avx2.Add(product3, sum3);

					accumulator0 = result2;
					accumulator1 = result3;
				}
			}

			if (stripe < stripeCount) {
				PrefetchNonTemporalNext(lData + (stripe * StripeLength) + 0x040);
				{
					byte* llData = lData + (stripe * StripeLength);
					byte* llSecret = lSecret + (stripe * 8u);

					var dataVec0 = *(Vector256<ulong>*)(llData + 0x00u);
					var dataVec1 = *(Vector256<ulong>*)(llData + 0x20u);
					var keyVec0 = *(Vector256<ulong>*)(llSecret + 0x00u);
					var keyVec1 = *(Vector256<ulong>*)(llSecret + 0x20u);

					var dataKey0 = Avx2.Xor(dataVec0, keyVec0);
					var dataKey1 = Avx2.Xor(dataVec1, keyVec1);
					var dataKeyLo0 = Avx2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
					var dataKeyLo1 = Avx2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
					var product0 = Avx2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
					var product1 = Avx2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);

					var dataSwap0 = Avx2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
					var dataSwap1 = Avx2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
					var addend0 = accumulator0;
					var addend1 = accumulator1;
					var sum0 = Avx2.Add(addend0, dataSwap0.AsUInt64());
					var sum1 = Avx2.Add(addend1, dataSwap1.AsUInt64());

					var result0 = Avx2.Add(product0, sum0);
					var result1 = Avx2.Add(product1, sum1);

					accumulator0 = result0;
					accumulator1 = result1;
				}
			}
		}

		{
			byte* lData = data + (length - StripeLength);
			byte* lSecret = secret + (SecretLength - StripeLength - 7u);

			var dataVec0 = *(Vector256<ulong>*)(lData + 0x00u);
			var dataVec1 = *(Vector256<ulong>*)(lData + 0x20u);
			var keyVec0 = *(Vector256<ulong>*)(lSecret + 0x00u);
			var keyVec1 = *(Vector256<ulong>*)(lSecret + 0x20u);

			var dataKey0 = Avx2.Xor(dataVec0, keyVec0);
			var dataKey1 = Avx2.Xor(dataVec1, keyVec1);
			var dataKeyLo0 = Avx2.Shuffle(dataKey0.AsUInt32(), ShuffleDataKey);
			var dataKeyLo1 = Avx2.Shuffle(dataKey1.AsUInt32(), ShuffleDataKey);
			var product0 = Avx2.Multiply(dataKey0.AsUInt32(), dataKeyLo0);
			var product1 = Avx2.Multiply(dataKey1.AsUInt32(), dataKeyLo1);

			var dataSwap0 = Avx2.Shuffle(dataVec0.AsUInt32(), ShuffleDataSwap);
			var dataSwap1 = Avx2.Shuffle(dataVec1.AsUInt32(), ShuffleDataSwap);
			var addend0 = accumulator0;
			var addend1 = accumulator1;
			var sum0 = Avx2.Add(addend0, dataSwap0.AsUInt64());
			var sum1 = Avx2.Add(addend1, dataSwap1.AsUInt64());

			var result0 = Avx2.Add(product0, sum0);
			var result1 = Avx2.Add(product1, sum1);

			accumulator0 = result0;
			accumulator1 = result1;
		}

		ulong result = unchecked(length * Prime64.Prime0);

		var data0 = Avx2.Xor(accumulator0, Vector256.Create(SecretValues64.Secret0B, SecretValues64.Secret13, SecretValues64.Secret1B, SecretValues64.Secret23));
		var data1 = Avx2.Xor(accumulator1, Vector256.Create(SecretValues64.Secret2B, SecretValues64.Secret33, SecretValues64.Secret3B, SecretValues64.Secret43));

		result += MixAccumulators(data0.GetElement(0), data0.GetElement(1));
		result += MixAccumulators(data0.GetElement(2), data0.GetElement(3));
		result += MixAccumulators(data1.GetElement(0), data1.GetElement(1));
		result += MixAccumulators(data1.GetElement(2), data1.GetElement(3));

		return Avalanche(result);
	}
}
