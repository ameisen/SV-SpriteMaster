using Benchmarks.BenchmarkBase;

namespace Benchmarks.Strings;

public sealed class Options : BenchmarkBase.Options {
	internal static Options Default => new();

	[Option("dictionary", "Dictionary for dictionary set")]
	public string Dictionary { get; set; } = @"D:\Stardew\SpriteMaster\Tests\Benchmarks\Hashing\bin\dictionary.zip";

	public Options() { }

	protected override void Process(string[] args) {
		base.Process(args);
	}
}
