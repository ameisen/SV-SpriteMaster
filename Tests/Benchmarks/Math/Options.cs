using Benchmarks.BenchmarkBase;

namespace Benchmarks.Math;

public sealed class Options : BenchmarkBase.Options {
	internal static Options Default => new();

	public Options() { }

	protected override void Process(string[] args) {
		base.Process(args);
	}
}
