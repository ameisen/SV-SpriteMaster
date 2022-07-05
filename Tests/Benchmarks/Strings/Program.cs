using Benchmarks.BenchmarkBase;
using System.Text.RegularExpressions;

namespace Benchmarks.Strings;

public class Program : ProgramBase<Options> {
	private static Action<Regex>? GetExternalTest(Regex pattern) {
		return null;
	}

	public static int Main(string[] args) {
		return MainBase(typeof(Program), args, GetExternalTest);
	}
}
