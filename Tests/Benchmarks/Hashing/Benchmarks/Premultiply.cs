using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using Microsoft.Toolkit.HighPerformance;
using Microsoft.Xna.Framework;
using SkiaSharp;
using SpriteMaster.Types;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using static Hashing.Benchmarks.Premultiply;

namespace Hashing.Benchmarks;
public class Premultiply : BenchmarkBase<Premultiply.SpriteDataSet, SpriteData[]> {
	public class SpriteData : IDisposable {
		internal readonly string Path;
		public Memory<byte> Data { get; private set; }
		public Span<byte> Span => Data.Span;
		internal object? TempReference = null;
		internal readonly byte[] Reference;
		internal SKBitmap Image;

		public void Setup() {
			TempReference = null;
			Data = new((byte[])Reference.Clone());
		}

		public void Cleanup() {
			TempReference = null;
		}

		internal SpriteData(string path, ReadOnlyMemory<byte> data, SKBitmap image) {
			Path = path;
			Reference = GC.AllocateUninitializedArray<byte>((int)data.Length);
			data.CopyTo(Reference);
			Data = new(Reference);
			Image = image;
		}

		public void Dispose() {
			Image?.Dispose();
		}
	}

	public readonly struct SpriteDataSet : IDataSet<SpriteData[]> {
		public readonly SpriteData[] Data { get; }

		private readonly uint Index => (Data.Length == 0) ? 0u : (uint)BitOperations.Log2((uint)Data.Length) + 1u;

		internal SpriteDataSet(ReadOnlySpan<SpriteData> data) {
			Data = data.ToArray();
		}

		public override readonly string ToString() => $"{Data.Length}";
	}

	static Premultiply() {
		const string ContentRoot = @"D:\Stardew\Reference\Content";
		const string ModRoot = @"D:\Stardew\root_mods";

		var allImages = new[] { ContentRoot, ModRoot }.SelectMany(dir => Directory.EnumerateFiles(dir, "*.png", SearchOption.AllDirectories)).ToArray();

		ConcurrentBag<SpriteData> allSpriteDatas = new();


		Parallel.ForEach(
			allImages, image => {
				using FileStream stream = File.OpenRead(image);
				SKBitmap bitmap = SKBitmap.Decode(stream);
				if (bitmap is not null) {
					var data = bitmap.Bytes;
					allSpriteDatas.Add(new(image, data, bitmap));
				}
			}
		);

		var dataList = allSpriteDatas.OrderBy(sd => sd.Path).ToArray();

		DataSets.Add(new(dataList));
	}

	[IterationSetup]
	public void IterationSetup() {
		foreach (var dataSet in DataSets) {
			foreach (var data in dataSet.Data) {
				data.Setup();
			}
		}
	}

	[IterationCleanup]
	public void IterationCleanup() {
		foreach (var dataSet in DataSets) {
			foreach (var data in dataSet.Data) {
				data.Cleanup();
			}
		}
	}

	[Benchmark(Description = "SkiaSharp", Baseline = true)]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public void Skia(in SpriteDataSet dataSet) {
		foreach (var data in dataSet.Data) {
			var rawPixels = SKPMColor.PreMultiply(data.Image.Pixels);

			var pixels = GC.AllocateUninitializedArray<Color>(rawPixels.Length);
			for (int i = 0; i < pixels.Length; i++) {
				SKPMColor pixel = rawPixels[i];
				pixels[i] = pixel.Alpha == 0
					? Color.Transparent
					: new Color(r: pixel.Red, g: pixel.Green, b: pixel.Blue, alpha: pixel.Alpha);
			}

			data.TempReference = pixels;
		}
	}

	[Benchmark(Description = "SkiaSharp (No Copy)")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public void SkiaNoCopy(in SpriteDataSet dataSet) {
		foreach (var data in dataSet.Data) {
			var rawPixels = SKPMColor.PreMultiply(data.Image.Pixels);

			var pixels = rawPixels.AsSpan().Cast<SKPMColor, Color>();
			for (int i = 0; i < pixels.Length; i++) {
				SKPMColor pixel = rawPixels[i];
				pixels[i] = pixel.Alpha == 0
					? Color.Transparent
					: new Color(r: pixel.Red, g: pixel.Green, b: pixel.Blue, alpha: pixel.Alpha);
			}

			data.TempReference = rawPixels;
		}
	}

	[Benchmark(Description = "Scalar (SMAPI)")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public void ScalarSMAPI(in SpriteDataSet dataSet) {
		foreach (var data in dataSet.Data) {
			ProcessTextureScalarSMAPI(data.Span.Cast<byte, Color8>());
		}
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void ProcessTextureScalarSMAPI(Span<Color8> data) {
		for (int i = 0; i < data.Length; i++) {
			var pixel = data[i];
			var alpha = pixel.A;

			// Transparent to zero (what XNA and MonoGame do)
			if (alpha == 0) {
				data[i].Packed = 0;
				continue;
			}

			// Premultiply
			if (alpha == byte.MaxValue)
				continue; // no need to change fully transparent/opaque pixels

			data[i] = new(
				pixel.R * pixel.A / byte.MaxValue,
				pixel.G * pixel.A / byte.MaxValue,
				pixel.B * pixel.A / byte.MaxValue,
				pixel.A
			);
		}
	}

	[Benchmark(Description = "Scalar")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public void Scalar(in SpriteDataSet dataSet) {
		foreach (var data in dataSet.Data) {
			SpriteMaster.Harmonize.Patches.FileCache.ProcessTextureScalar(data.Span.Cast<byte, Color8>());
		}
	}

	[Benchmark(Description = "SSE2")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public void Sse2(in SpriteDataSet dataSet) {
		foreach (var data in dataSet.Data) {
			SpriteMaster.Harmonize.Patches.FileCache.ProcessTextureSse2(data.Span.Cast<byte, Color8>());
		}
	}

	[Benchmark(Description = "SSE2 (Unrolled)")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public void Sse2Unrolled(in SpriteDataSet dataSet) {
		foreach (var data in dataSet.Data) {
			SpriteMaster.Harmonize.Patches.FileCache.ProcessTextureSse2Unrolled(data.Span.Cast<byte, Color8>());
		}
	}

	[Benchmark(Description = "AVX2")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public void Avx2(in SpriteDataSet dataSet) {
		foreach (var data in dataSet.Data) {
			SpriteMaster.Harmonize.Patches.FileCache.ProcessTextureAvx2(data.Span.Cast<byte, Color8>());
		}
	}

	[Benchmark(Description = "AVX2 (Unrolled)")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public void Avx2Unrolled(in SpriteDataSet dataSet) {
		foreach (var data in dataSet.Data) {
			SpriteMaster.Harmonize.Patches.FileCache.ProcessTextureAvx2Unrolled(data.Span.Cast<byte, Color8>());
		}
	}
}
