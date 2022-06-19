using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Hashing.Benchmarks;
using SpriteMaster.Extensions;
using System.Diagnostics;

namespace Hashing;

public class Program {
	internal static Options Options { get; private set; } = Options.Default;

	private static ((Summary, IConfig, Job)?, (Summary, IConfig, Job)?)? ConditionalRun<TBenchmark>(Options options, GCType gcType, Runtime runtime) where TBenchmark : BenchmarkBase {
		if (!options.Set.Contains(typeof(TBenchmark).Name)) {
			return null;
		}

		if (Options.Cold) {
			return (
				ConditionalRun<TBenchmark>(options, gcType, runtime, coldStart: true),
				ConditionalRun<TBenchmark>(options, gcType, runtime, coldStart: false)
			);
		}
		else {
			return (
				null,
				ConditionalRun<TBenchmark>(options, gcType, runtime, coldStart: false)
			);
		}
	}

	private static (Summary, IConfig, Job)? ConditionalRun<TBenchmark>(Options options, GCType gcType, Runtime runtime, bool coldStart) where TBenchmark : BenchmarkBase {
		if (!options.Set.Contains(typeof(TBenchmark).Name)) {
			return null;
		}

		var config = DefaultConfig.Instance.WithOptions(ConfigOptions.Default);
		if (Debugger.IsAttached) {
			config = config.WithOptions(ConfigOptions.DisableOptimizationsValidator);
		}

		if (options.Runners.Count != 0) {
			var filters = options.Runners.Select(runner => new NameFilter(name => name.EqualsInvariantInsensitive(runner))).Cast<IFilter>();

			var disjunction = new DisjunctionFilter(filters.ToArray());

			config = config.AddFilter(disjunction);
		}

		if (options.DiagnoseMemory) {
			config = config.AddDiagnoser(MemoryDiagnoser.Default);
		}

		if (options.DiagnoseInlining) {
			config = config.AddDiagnoser(new InliningDiagnoser());
		}

		if (options.DiagnoseTailCall) {
			config = config.AddDiagnoser(new TailCallDiagnoser());
		}

		if (options.DiagnoseEtw) {
			config = config.AddDiagnoser(new EtwProfiler());
		}

		//config.AddJob(Job.InProcess);

		Job job;

		if (Debugger.IsAttached) {
			job = Job.InProcess
				.WithGcServer(gcType == GCType.Server)
				.WithRuntime(runtime)
				.WithEnvironmentVariables(
					new EnvironmentVariable("DOTNET_TieredCompilation", "0")
				)
				.WithStrategy(coldStart ? RunStrategy.ColdStart : RunStrategy.Throughput);
		}
		else {
			job = Job.Default
				.WithGcServer(gcType == GCType.Server)
				.WithRuntime(runtime)
				.WithEnvironmentVariables(
					new EnvironmentVariable("DOTNET_TieredCompilation", "0")
				)
				.WithStrategy(coldStart ? RunStrategy.ColdStart : RunStrategy.Throughput);
		}

		if (typeof(TBenchmark) == typeof(Benchmarks.Premultiply)) {
			job = job.WithMinIterationCount(60).WithMaxIterationCount(400);
		}

		string name = $"{gcType}.{runtime}.{(coldStart ? "cold" : "warm")}";

		config = config.AddJob(job);
		config = config.WithArtifactsPath(Path.Combine(config.ArtifactsPath, name));

		System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(TBenchmark).TypeHandle);

		var summary = BenchmarkRunner.Run<TBenchmark>(config);

		return (summary, config, job);
	}

	readonly record struct OptionPermutation(GCType GCType, Runtime Runtime);

	public static int Main(string[] args) {
		var options = Options.From(args);
		Options = options;

		//options.GCTypes.Add(GCType.Workstation);
		//options.Runtimes.Remove(CoreRtRuntime.CoreRt50);


		if (options.GCTypes.Count == 0) {
			options.GCTypes.Add(GCType.Workstation);
		}

		if (options.Runtimes.Count == 0) {
			options.Runtimes.Add(CoreRuntime.Core50);
		}

		HashSet<OptionPermutation> optionPermutations = new();

		foreach (var gcType in options.GCTypes) {
			foreach (var runtime in options.Runtimes) {
				optionPermutations.Add(new(gcType, runtime));
			}
		}

		foreach (var optionPermutation in optionPermutations) {
			_ = ConditionalRun<Benchmarks.Buffers>(options, optionPermutation.GCType, optionPermutation.Runtime);
			_ = ConditionalRun<Benchmarks.Strings>(options, optionPermutation.GCType, optionPermutation.Runtime);
			_ = ConditionalRun<Benchmarks.Dictionary>(options, optionPermutation.GCType, optionPermutation.Runtime);
			_ = ConditionalRun<Benchmarks.Sprites>(options, optionPermutation.GCType, optionPermutation.Runtime);
			_ = ConditionalRun<Benchmarks.Premultiply>(options, optionPermutation.GCType, optionPermutation.Runtime);
		}

		return 0;
	}
}