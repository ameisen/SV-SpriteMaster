using BenchmarkDotNet.Running;

namespace Texture;

public class Texture {
	//[STAThread]
	public static int Main(string[] args) {
		_ = BenchmarkRunner.Run<Tests>();
		return 0;
	}
}