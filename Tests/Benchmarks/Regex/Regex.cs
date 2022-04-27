using BenchmarkDotNet.Running;

namespace Regex;

public static class Regex {
	//[STAThread]
	public static int Main(string[] args) {
		var summary = BenchmarkRunner.Run<Benchmarks>();
		return 0;
	}
}