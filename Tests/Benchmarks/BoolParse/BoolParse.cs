using BenchmarkDotNet.Running;

namespace BoolParse;

public class BoolParse {
	//[STAThread]
	public static int Main(string[] args) {
		_ = BenchmarkRunner.Run<IsBool>();
		_ = BenchmarkRunner.Run<CheckValue>();
		return 0;
	}
}