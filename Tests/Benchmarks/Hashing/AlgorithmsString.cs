using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Microsoft.Toolkit.HighPerformance;
using SpriteMaster.Extensions;
using SpriteMaster.Hashing.Algorithms;
using System;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Hashing;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.Default, MethodOrderPolicy.Alphabetical)]
//[InliningDiagnoser(true, true)]
//[TailCallDiagnoser]
//[EtwProfiler]
//[SimpleJob(RuntimeMoniker.CoreRt50)]
public class AlgorithmsString {
	private const int RandSeed = 0x13377113;
	private const int MinSize = 1;
	private const int MaxSize = 65_536;

	public readonly struct DataSet {
		public readonly string Data;

		public DataSet(string data) => Data = data;

		public override string ToString() => Data.Length.ToString();
	}

	public List<DataSet> DataSets { get; init; } = new();

	public int currentDataSet;

	private static readonly char[] Chars =
		"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();


	public AlgorithmsString() {
		var random = new Random(RandSeed);

		unsafe void AddSet(in DataSet dataSet) {
			DataSets.Add(dataSet);

			var referenceHash = XxHash3.Hash64(dataSet.Data);
			ulong cppHash;
			var dataSpan = dataSet.Data.AsSpan().AsBytes();
			fixed (byte* data = dataSpan) {
				cppHash = XXH3_64bits(data, (ulong)dataSpan.Length);
			}

			if (referenceHash != cppHash) {
				Console.Error.WriteLine($"Hashes Not Equal ({dataSpan.Length}) : {referenceHash} != {cppHash}");
			}
		}

		{
			var sb = new StringBuilder();
			for (int j = 0; j < 0; ++j) {
				sb.Append(Chars[random.Next(0, Chars.Length)]);
			}

			AddSet(new(sb.ToString()));
		}

		for (int i = MinSize; i <= MaxSize; i *= 2) {
			var sb = new StringBuilder();
			for (int j = 0; j < i; ++j) {
				sb.Append(Chars[random.Next(0, Chars.Length)]);
			}

			AddSet(new(sb.ToString()));
		}

		if (DataSets.Last().Data.Length != MaxSize) {
			var sb = new StringBuilder();
			for (int j = 0; j < MaxSize; ++j) {
				sb.Append(Chars[random.Next(0, Chars.Length)]);
			}

			AddSet(new(sb.ToString()));
		}
	}

	// return Marvin.ComputeHash32(ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(value)), (uint)value.Length * 2 /* in bytes, not chars */, (uint)seed, (uint)(seed >> 32));
	// seed0 = (5381<<16) + 5381
	// seed1 = 5381




	[Benchmark(Description = "xxHash3")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public ulong xxHash3(DataSet dataSet) {
		return XxHash3.Hash64(dataSet.Data);
	}

	/*
	[Benchmark(Description = "DJB2")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public int DJB2(in DataSet dataSet) {
		return dataSet.Data.GetDjb2HashCode();
	}

	[Benchmark(Description = "GetHashCode")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public int HashCode(in DataSet dataSet) {
		return dataSet.Data.GetHashCode();
	}
	*/

	[DllImport("libxxhash.dll", CallingConvention = CallingConvention.Cdecl)]
	private static extern unsafe ulong XXH3_64bits(void* input, ulong length);
	// XXH_PUBLIC_API XXH_PUREF XXH64_hash_t XXH3_64bits(const void* input, size_t length);

	[Benchmark(Description = "xxHash3 C++")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public unsafe ulong xxHash3CPP(DataSet dataSet) {
		var span = dataSet.Data.AsSpan().AsBytes();
		fixed (byte* data = span) {
			return XXH3_64bits(data, (ulong)span.Length);
		}
	}

#if false

	private delegate int MarvinHash32Delegate(ReadOnlySpan<byte> data, ulong seed);

	private const ulong MarvinHash32Seed = 0x0000_1505_1505_1505UL;

	private static readonly MethodInfo ComputeHash32Method = Type.GetType("System.Marvin")?.GetMethod(
		"ComputeHash32",
		BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
		null,
		new Type[] { typeof(ReadOnlySpan<byte>), typeof(ulong) },
		null
	) ?? throw new MissingMethodException("ComputeHash32");

	private static readonly MarvinHash32Delegate MarvinHash32Core = ComputeHash32Method.CreateDelegate<MarvinHash32Delegate>();

	[Benchmark(Description = "Marvin32")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public int Marvin32(in DataSet dataSet) {
		return MarvinHash32Core(
			dataSet.Data.AsSpan().AsBytes(),
			MarvinHash32Seed
		);
	}

	private static uint NormanHash(ReadOnlySpan<byte> data) {
		uint hash = 0U;
		foreach (var octet in data) {
			hash = BitOperations.RotateRight(hash, 4) + octet;
		}
		return hash;
	}

	[Benchmark(Description = "NormanHash")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public uint NormanHash(in DataSet dataSet) {
		return NormanHash(dataSet.Data.AsSpan().AsBytes());
	}

	private static uint NormanHashUnrolled(ReadOnlySpan<byte> data) {
		uint hash = 0U;

		int index = 0;
		while (data.Length - index >= 8) {
			var octet0 = data[index + 0];
			var octet1 = data[index + 1];
			var octet2 = data[index + 2];
			var octet3 = data[index + 3];
			var octet4 = data[index + 4];
			var octet5 = data[index + 5];
			var octet6 = data[index + 6];
			var octet7 = data[index + 7];
			hash = BitOperations.RotateRight(hash, 4) + octet0;
			hash = BitOperations.RotateRight(hash, 4) + octet1;
			hash = BitOperations.RotateRight(hash, 4) + octet2;
			hash = BitOperations.RotateRight(hash, 4) + octet3;
			hash = BitOperations.RotateRight(hash, 4) + octet4;
			hash = BitOperations.RotateRight(hash, 4) + octet5;
			hash = BitOperations.RotateRight(hash, 4) + octet6;
			hash = BitOperations.RotateRight(hash, 4) + octet7;
			index += 8;
		}
		while (data.Length - index > 0) {
			hash = BitOperations.RotateRight(hash, 4) + data[index++];
		}

		return hash;
	}

	[Benchmark(Description = "NormanHash (Unrolled)")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public uint NormanHashUnrolled(in DataSet dataSet) {
		return NormanHashUnrolled(dataSet.Data.AsSpan().AsBytes());
	}
#endif
}
