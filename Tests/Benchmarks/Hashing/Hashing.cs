using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Hashing;

public class Hashing {
	//[STAThread]
	public static int Main(string[] args) {
		var config = DefaultConfig.Instance
			.WithOptions(ConfigOptions.Default)
			.WithOptions(ConfigOptions.DisableOptimizationsValidator);
		//_ = BenchmarkRunner.Run<Algorithms>(config);
		_ = BenchmarkRunner.Run<AlgorithmsString>(config);
		return 0;
	}
}