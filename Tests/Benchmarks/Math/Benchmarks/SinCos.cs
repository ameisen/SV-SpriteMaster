using BenchmarkDotNet.Attributes;
using Benchmarks.BenchmarkBase.Benchmarks;
using Benchmarks.Math.Benchmarks.Sources;
using SpriteMaster.Extensions;

namespace Benchmarks.Math.Benchmarks;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
public class SinCos : RandomReals {
	[Benchmark(Description = "SinCosF", Baseline = true)]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public float SinCosF(in DataSet<RealData[]> dataSet) {
		float sinSum = 0.0f;
		float cosSum = 0.0f;

		foreach (var real in dataSet.Data) {
			var (sin, cos) = MathExt.SinCos(real.Single);
			sinSum += sin;
			cosSum += cos;
		}

		return sinSum + cosSum;
	}

	[Benchmark(Description = "SinCosD")]
	[ArgumentsSource(nameof(DataSets), Priority = 0)]
	public double SinCosD(in DataSet<RealData[]> dataSet) {
		double sinSum = 0.0;
		double cosSum = 0.0;

		foreach (var real in dataSet.Data) {
			var (sin, cos) = MathExt.SinCos(real.Double);
			sinSum += sin;
			cosSum += cos;
		}

		return sinSum + cosSum;
	}
}
