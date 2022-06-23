using Benchmarks.BenchmarkBase;

namespace Benchmarks.Hashing;

public class Program : ProgramBase<Options> {
	public static int Main(string[] args) {
		return MainBase(typeof(Program), args);
	}
}