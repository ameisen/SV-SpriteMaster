namespace Benchmarks.Math.Benchmarks.Sources;

public abstract class RandomReals : RealSource {
	private const int RandSeed = 0x13377113;
	private static readonly long Size = 0x10000;

	static RandomReals() {
		var random = new Random(RandSeed);

		List<double> values = new((int)Size);

		for (long i = 0; i < Size; ++i) {
			values.Add((random.NextDouble() * 100000.0) - 50000.0);
		}

		AddSet(values);
	}
}
