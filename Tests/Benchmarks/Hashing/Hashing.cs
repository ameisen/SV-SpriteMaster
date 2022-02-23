using BenchmarkDotNet.Running;

namespace Hashing;

public class Hashing {
	//[STAThread]
	public static int Main(string[] args) {
		var summary = BenchmarkRunner.Run<Algorithms>();
		return 0;
	}
}