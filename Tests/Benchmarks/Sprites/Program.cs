using Benchmarks.BenchmarkBase;
using Benchmarks.Sprites.Benchmarks;
using System.Text.RegularExpressions;

namespace Benchmarks.Sprites;

public class Program : ProgramBase<Options> {
	private static Action<Regex>? GetExternalTest(Regex pattern) {
		if (pattern.IsMatch(nameof(Quality))) {
			return _ => new Quality().Test();
		}

		return null;
	}

	public static int Main(string[] args) {
		return MainBase(typeof(Program), args, GetExternalTest);
	}
}
