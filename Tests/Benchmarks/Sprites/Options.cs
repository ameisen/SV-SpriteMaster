using Benchmarks.BenchmarkBase;

namespace Benchmarks.Sprites;

public sealed class Options : BenchmarkBase.Options {
	internal static Options Default => new();


	[Option("short", "Use Short Test Set")]
	public bool Short { get; set; } = false;


	[Option("small", "Use Small Test Set")]
	public bool Small { get; set; } = false;

	public Options() { }

	protected override void Process(string[] args) {
		base.Process(args);
	}
}
