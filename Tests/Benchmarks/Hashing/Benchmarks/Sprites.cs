using BenchmarkDotNet.Attributes;
using Microsoft.Toolkit.HighPerformance;
using SpriteMaster.Types;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using static Hashing.Benchmarks.Sprites;

namespace Hashing.Benchmarks;
public class Sprites : BenchmarkBase<SpriteDataSet, ReadOnlyMemory2D<uint>> {
	private const int RandSeed = 0x13377113;

	private static SpriteDataSet MakeAlignedSprite(Random random, Vector2I size) {
		var data = GC.AllocateUninitializedArray<uint>(size.Area);
		var dataBytes = data.AsSpan().AsBytes();
		random.NextBytes(dataBytes);
		return new(
			true,
			new(
				data,
				height: size.Height,
				width: size.Width
			)
		);
	}

	private static SpriteDataSet MakeUnalignedSprite(Random random, Vector2I size, Vector2I innerSize) {
		Vector2I offset = (size - innerSize) / 2;
		int offsetIndex = (offset.Height * size.Width) + offset.Width;

		var data = GC.AllocateUninitializedArray<uint>(size.Area);
		var dataBytes = data.AsSpan().AsBytes();
		random.NextBytes(dataBytes);
		return new(
			false,
			new(
				data,
				width: innerSize.Width,
				height: innerSize.Height,
				offset: offsetIndex,
				pitch: size.Width - innerSize.Width
			)
		);
	}

	public readonly struct SpriteDataSet : IDataSet<ReadOnlyMemory2D<uint>> {
		public readonly ReadOnlyMemory2D<uint> Data { get; }
		internal readonly bool Aligned;

		internal ReadOnlySpan2D<uint> Span => Data.Span;

		private readonly uint Index => (Data.Length == 0) ? 0u : (uint)BitOperations.Log2((uint)Data.Length) + 1u;

		internal SpriteDataSet(bool aligned, Memory2D<uint> data) {
			Data = data;
			Aligned = aligned;
		}

		public override readonly string ToString() => $"{Data.Width}x{Data.Height} {(Aligned ? "Aligned" : "Unaligned")}";
	}

	private static readonly Vector2I[] CommonSizes = {
		(48, 32),
		(16, 16),
		(16, 32),
		(32, 16),
		(32, 32),
		(48, 32),
		(16, 64),
		(64, 16),
		(64, 64),
		(16 * 3, 16 * 5)
	};

	static Sprites() {
		var random = new Random(RandSeed);

		foreach (var size in CommonSizes) {
			DataSets.Add(MakeAlignedSprite(random, size));
		}

		Vector2I outerSize = (1024, 1024);
		foreach (var size in CommonSizes) {
			DataSets.Add(MakeUnalignedSprite(random, outerSize, size));
		}

		if (Program.Options.DoValidate) {
			Console.WriteLine("Performing Validation...");

			var referenceInstance = new Sprites();

			foreach (var dataSet in DataSets) {
				try {
					ulong copyHash = referenceInstance.Copy(dataSet);
					ulong segmentedHash = referenceInstance.SegmentedSpan(dataSet);

					if (copyHash != segmentedHash) {
						Console.Error.WriteLine($"CopyHash and SegmentedHash mismatch [{dataSet}]: {copyHash} != {segmentedHash}");
					}
				}
				catch (Exception ex) {
					Console.Error.WriteLine($"Exception validating {dataSet}: {ex.GetType().Name} {ex.Message}");
				}
			}
		}
	}

	[Benchmark(Description = "Row by Row")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public ulong RowByRow(in SpriteDataSet dataSet) {
		var span = dataSet.Span;

		ulong hash = 0;
		for (int i = 0; i < span.Height; ++i) {
			var row = span.GetRowSpan(i);
			hash = SpriteMaster.Hashing.HashUtility.Accumulate(hash, SpriteMaster.Hashing.Algorithms.XxHash3.Hash64(row.AsBytes()));
		}

		return hash;
	}

	private static unsafe void FastCopyTo<T>(ReadOnlySpan<T> source, Span<T> dest) where T : unmanaged {
		int remaining = source.Length * Unsafe.SizeOf<T>();

		fixed (T* srcP = source) {
			fixed (T* dstP = dest) {
				byte* src = (byte*)srcP;
				byte* dst = (byte*)dstP;

				for (; remaining >= 512; remaining -= 512) {
					var vector0 = Avx.LoadVector256((ulong*)(src + 0x000));
					var vector1 = Avx.LoadVector256((ulong*)(src + 0x020));
					var vector2 = Avx.LoadVector256((ulong*)(src + 0x040));
					var vector3 = Avx.LoadVector256((ulong*)(src + 0x060));
					var vector4 = Avx.LoadVector256((ulong*)(src + 0x080));
					var vector5 = Avx.LoadVector256((ulong*)(src + 0x0A0));
					var vector6 = Avx.LoadVector256((ulong*)(src + 0x0C0));
					var vector7 = Avx.LoadVector256((ulong*)(src + 0x0E0));
					var vector8 = Avx.LoadVector256((ulong*)(src + 0x100));
					var vector9 = Avx.LoadVector256((ulong*)(src + 0x120));
					var vectorA = Avx.LoadVector256((ulong*)(src + 0x140));
					var vectorB = Avx.LoadVector256((ulong*)(src + 0x160));
					var vectorC = Avx.LoadVector256((ulong*)(src + 0x180));
					var vectorD = Avx.LoadVector256((ulong*)(src + 0x1A0));
					var vectorE = Avx.LoadVector256((ulong*)(src + 0x1C0));
					var vectorF = Avx.LoadVector256((ulong*)(src + 0x1E0));

					Avx.Store((ulong*)(dst + 0x000), vector0);
					Avx.Store((ulong*)(dst + 0x020), vector1);
					Avx.Store((ulong*)(dst + 0x040), vector2);
					Avx.Store((ulong*)(dst + 0x060), vector3);
					Avx.Store((ulong*)(dst + 0x080), vector4);
					Avx.Store((ulong*)(dst + 0x0A0), vector5);
					Avx.Store((ulong*)(dst + 0x0C0), vector6);
					Avx.Store((ulong*)(dst + 0x0E0), vector7);
					Avx.Store((ulong*)(dst + 0x100), vector8);
					Avx.Store((ulong*)(dst + 0x120), vector9);
					Avx.Store((ulong*)(dst + 0x140), vectorA);
					Avx.Store((ulong*)(dst + 0x160), vectorB);
					Avx.Store((ulong*)(dst + 0x180), vectorC);
					Avx.Store((ulong*)(dst + 0x1A0), vectorD);
					Avx.Store((ulong*)(dst + 0x1C0), vectorE);
					Avx.Store((ulong*)(dst + 0x1E0), vectorF);

					src += 512;
					dst += 512;
				}

				if (remaining >= 256) {
					var vector0 = Avx.LoadVector256((ulong*)(src + 0x000));
					var vector1 = Avx.LoadVector256((ulong*)(src + 0x020));
					var vector2 = Avx.LoadVector256((ulong*)(src + 0x040));
					var vector3 = Avx.LoadVector256((ulong*)(src + 0x060));
					var vector4 = Avx.LoadVector256((ulong*)(src + 0x080));
					var vector5 = Avx.LoadVector256((ulong*)(src + 0x0A0));
					var vector6 = Avx.LoadVector256((ulong*)(src + 0x0C0));
					var vector7 = Avx.LoadVector256((ulong*)(src + 0x0E0));

					Avx.Store((ulong*)(dst + 0x000), vector0);
					Avx.Store((ulong*)(dst + 0x020), vector1);
					Avx.Store((ulong*)(dst + 0x040), vector2);
					Avx.Store((ulong*)(dst + 0x060), vector3);
					Avx.Store((ulong*)(dst + 0x080), vector4);
					Avx.Store((ulong*)(dst + 0x0A0), vector5);
					Avx.Store((ulong*)(dst + 0x0C0), vector6);
					Avx.Store((ulong*)(dst + 0x0E0), vector7);

					remaining -= 256;
					src += 256;
					dst += 256;
				}

				if (remaining >= 128) {
					var vector0 = Avx.LoadVector256((ulong*)(src + 0x000));
					var vector1 = Avx.LoadVector256((ulong*)(src + 0x020));
					var vector2 = Avx.LoadVector256((ulong*)(src + 0x040));
					var vector3 = Avx.LoadVector256((ulong*)(src + 0x060));

					Avx.Store((ulong*)(dst + 0x000), vector0);
					Avx.Store((ulong*)(dst + 0x020), vector1);
					Avx.Store((ulong*)(dst + 0x040), vector2);
					Avx.Store((ulong*)(dst + 0x060), vector3);

					remaining -= 128;
					src += 128;
					dst += 128;
				}

				if (remaining >= 64) {
					var vector0 = Avx.LoadVector256((ulong*)(src + 0x000));
					var vector1 = Avx.LoadVector256((ulong*)(src + 0x020));

					Avx.Store((ulong*)(dst + 0x000), vector0);
					Avx.Store((ulong*)(dst + 0x020), vector1);

					remaining -= 64;
					src += 64;
					dst += 64;
				}

				if (remaining >= 32) {
					var vector0 = Avx.LoadVector256((ulong*)(src + 0x000));

					Avx.Store((ulong*)(dst + 0x000), vector0);

					remaining -= 32;
					src += 32;
					dst += 32;
				}

				if (remaining >= 16) {
					var vector0 = Sse2.LoadVector128((ulong*)(src + 0x000));

					Sse2.Store((ulong*)(dst + 0x000), vector0);

					remaining -= 16;
					src += 16;
					dst += 16;
				}

				for (; remaining > 0; --remaining) {
					*(dst++) = *(src++);
				}
			}
		}
	}

	private static void FastCopyTo(ReadOnlySpan2D<uint> source, Span<uint> dest) {
		int destOffset = 0;

		for (int i = 0; i < source.Height; ++i) {
			var row = source.GetRowSpan(i);

			FastCopyTo(row, dest.Slice(destOffset, row.Length));

			destOffset += row.Length;
		}
	}

	private static void CopyTo(ReadOnlySpan2D<uint> source, Span<uint> dest) {
		int destOffset = 0;
		
		for (int i = 0; i < source.Height; ++i) {
			var row = source.GetRowSpan(i);

			row.CopyTo(dest.Slice(destOffset, row.Length));

			destOffset += row.Length;
		}
	}

	[Benchmark(Description = "Copy")]
	[ArgumentsSource(nameof(DataSets), Priority = 1)]
	public ulong Copy(in SpriteDataSet dataSet) {
		var span = dataSet.Span;

		var copiedData = GC.AllocateUninitializedArray<uint>((int)span.Length);
		span.CopyTo(copiedData);

		return SpriteMaster.Hashing.Algorithms.XxHash3.Hash64(copiedData.AsSpan().AsBytes());
	}

	[Benchmark(Description = "Copy 2")]
	[ArgumentsSource(nameof(DataSets), Priority = 1)]
	public ulong Copy2(in SpriteDataSet dataSet) {
		var span = dataSet.Span;

		var copiedData = GC.AllocateUninitializedArray<uint>((int)span.Length);
		CopyTo(span, copiedData);

		return SpriteMaster.Hashing.Algorithms.XxHash3.Hash64(copiedData.AsSpan().AsBytes());
	}

	[Benchmark(Description = "FastCopy")]
	[ArgumentsSource(nameof(DataSets), Priority = 1)]
	public ulong CopyFast(in SpriteDataSet dataSet) {
		var span = dataSet.Span;

		var copiedData = GC.AllocateUninitializedArray<uint>((int)span.Length);
		FastCopyTo(span, copiedData);

		return SpriteMaster.Hashing.Algorithms.XxHash3.Hash64(copiedData.AsSpan().AsBytes());
	}

	[Benchmark(Description = "SegmentedSpan")]
	[ArgumentsSource(nameof(DataSets), Priority = 2)]
	public ulong SegmentedSpan(in SpriteDataSet dataSet) {
		var span = dataSet.Span;

		return SpriteMaster.Hashing.Algorithms.XxHash3.Hash64(span);
	}
}
