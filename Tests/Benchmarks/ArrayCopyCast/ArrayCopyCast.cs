using BenchmarkDotNet.Running;

namespace ArrayCopyCast;

public class ArrayCopyCast {
	//[STAThread]
	public static int Main(string[] args) {
		var summary = BenchmarkRunner.Run<Algorithms>();
		return 0;
	}
}