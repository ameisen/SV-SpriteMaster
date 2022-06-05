using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Toolkit.HighPerformance;
using SpriteMaster.Hashing.Algorithms;
using System.Data.HashFunction.xxHash;

namespace Hashing;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.Declared, MethodOrderPolicy.Declared)]
[CsvExporter]
//[InliningDiagnoser(true, true)]
//[TailCallDiagnoser]
//[EtwProfiler]
//[SimpleJob(RuntimeMoniker.CoreRt50)]
public class Algorithms {
	private const int RandSeed = 0x13377113;
	private const int MinSize = 0x0;
	private const int MaxSize = 4096;

	public readonly struct DataSet<T> where T : unmanaged {
		public readonly T[] Data;

		public readonly uint Index => (Data.Length == 0) ? 0u : (uint)Math.Round(Math.Log2(Data.Length)) + 1u;

		public DataSet(T[] data) => Data = data;

		public override string ToString() => $"({Index:D2}) {Data.Length}";
	}

	public static List<DataSet<byte>> DataSets { get; }
	static Algorithms() {
		DataSets = new();

		var random = new Random(RandSeed);

		int start = MinSize;

		DataSet<byte> MakeDataSet(int length) {
			var data = new byte[length];
			random.NextBytes(data);

			return new(data);
		}

		if (start == 0) {
			var dataSet = MakeDataSet(start);

			DataSets.Add(dataSet);

			start = 1;
		}

		if (MaxSize == MinSize) {
			var dataSet = MakeDataSet(MaxSize);
			DataSets.Add(dataSet);
		}
		else {
			for (int i = start; i <= MaxSize; i *= 2) {
				var dataSet = MakeDataSet(i);

				DataSets.Add(dataSet);
			}
		}
	}

	[GlobalSetup(Target = nameof(xxHash3))]
	public void xxHash3() {
		XxHash3.UseAVX2 = true;
	}

	[Benchmark(Description = "xxHash3", Baseline = true)]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public ulong xxHash3(DataSet<byte> dataSet) {
		return XxHash3.Hash64(dataSet.Data);
	}

#if false
	[Benchmark(Description = "FNV1a")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public ulong FNV1a(in DataSet<byte> dataSet) {
		return Functions.FNV1a(dataSet.Data);
	}

	[Benchmark(Description = "DGB2-32")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public int DJB232(in DataSet<byte> dataSet) {
		return dataSet.Data.GetDjb2HashCode();
	}

	/*
	[Benchmark(Description = "CombHash")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public ulong CombHash(in DataSet<byte> dataSet) {
		return Functions.CombHash(dataSet.Data);
	}
	*/
#endif
}
