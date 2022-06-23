using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;
using LinqFasterer;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Benchmarks.BenchmarkBase;

[PublicAPI]
public abstract class ProgramBase<TOptions> where TOptions : Options, new() {
	private static TOptions? CurrentOptionsImpl = null!;

	public static TOptions CurrentOptions {
		get {
			if (CurrentOptionsImpl is not {} options) {
				var argsEnv = Environment.GetEnvironmentVariable("BENCHIE_ARGS") ?? "";
				var args = argsEnv.Split((string[]?)null, StringSplitOptions.RemoveEmptyEntries);
				options = CurrentOptionsImpl = Options.From<TOptions>(args);
			}

			return options;
		}
	}

	private static Regex[] CreatePatterns<TEnumerable>(TEnumerable patterns) where TEnumerable : IEnumerable<string> {
		return patterns.Select(Options.CreatePattern).Distinct().ToArray();
	}

	private static ((Summary, IConfig, Job)?, (Summary, IConfig, Job)?)? ConditionalRun(Type benchmarkType, string[] args, Options options, GCType gcType, Runtime runtime) {
		if (CurrentOptions.Cold) {
			return (
				ConditionalRun(benchmarkType, args, options, gcType, runtime, coldStart: true),
				ConditionalRun(benchmarkType, args, options, gcType, runtime, coldStart: false)
			);
		}
		else {
			return (
				null,
				ConditionalRun(benchmarkType, args, options, gcType, runtime, coldStart: false)
			);
		}
	}

	private static (Summary, IConfig, Job)? ConditionalRun(Type benchmarkType, string[] args, Options options, GCType gcType, Runtime runtime, bool coldStart) {
		var config = DefaultConfig.Instance.WithOptions(ConfigOptions.Default);
		//if (Debugger.IsAttached) {
			config = config.WithOptions(ConfigOptions.DisableOptimizationsValidator);
		//}

		if (options.Runners.Count != 0) {
			var patterns = CreatePatterns(options.Runners);

			var filters = patterns.Select(runner =>
				new NameFilter(runner.IsMatch)
			).Cast<IFilter>();

			var disjunction = new DisjunctionFilter(filters.ToArray());

			config = config.AddFilter(disjunction);
		}

		if (options.DiagnoseCpu) {
			config = config.AddDiagnoser(CpuDiagnoser.Default);
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

		if (Debugger.IsAttached || CurrentOptions.InProcess) {
			job = Job.InProcess
				.WithGcServer(gcType == GCType.Server)
				.WithRuntime(runtime)
				.WithEnvironmentVariables(
					new EnvironmentVariable("DOTNET_TieredCompilation", "0"),
					new EnvironmentVariable("BENCHIE_ARGS", string.Join(" ", args))
				)
				.WithStrategy(coldStart ? RunStrategy.ColdStart : RunStrategy.Throughput);
		}
		else {
			job = Job.Default
				.WithGcServer(gcType == GCType.Server)
				.WithRuntime(runtime)
				.WithEnvironmentVariables(
					new EnvironmentVariable("DOTNET_TieredCompilation", "0"),
					new EnvironmentVariable("BENCHIE_ARGS", string.Join(" ", args))
				)
				.WithStrategy(coldStart ? RunStrategy.ColdStart : RunStrategy.Throughput);
		}

		//if (typeof(TBenchmark) == typeof(Benchmarks.Premultiply)) {
		//	job = job.WithMinIterationCount(60).WithMaxIterationCount(400);
		//}

		string name = $"{gcType}.{runtime}.{(coldStart ? "cold" : "warm")}";

		config = config.AddJob(job);
		config = config.WithArtifactsPath(Path.Combine(config.ArtifactsPath, name));

		{
			List<Type> baseTypes = new();
			Type? baseType = benchmarkType;
			while (baseType is not null) {
				baseTypes.Add(baseType);
				baseType = baseType.BaseType;
			}

			foreach (var type in baseTypes.ReverseF()) {
				System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);
			}
		}

		GC.Collect(int.MaxValue, GCCollectionMode.Forced, blocking: true, compacting: true);

		var summary = BenchmarkRunner.Run(benchmarkType, config);

		return (summary, config, job);
	}

	readonly record struct OptionPermutation(GCType GCType, Runtime Runtime);

	public static int MainBase(Type rootType, string[] args, Func<Regex, Action<Regex>?>? execCallback = null) {
		var options = Options.From<TOptions>(args);
		CurrentOptionsImpl = options;

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

		var allBenchmarkTypes = rootType.Assembly.GetTypes()
			.WhereF(type =>
				!type.IsAbstract &&
				type.IsAssignableTo(typeof(Benchmarks.BenchmarkBase))
			).ToArrayF();

		var setPatterns = CreatePatterns(options.Set);

		var matchingBenchmarkTypes = allBenchmarkTypes
			.WhereF(type =>
				setPatterns.Any(pattern => pattern.IsMatch(type.Name))
			).ToArrayF();

		Dictionary<string, Action> externalSets = new();

		bool error = false;
		foreach (var setPattern in setPatterns) {
			if (!matchingBenchmarkTypes.AnyF(type => setPattern.IsMatch(type.Name))) {
				bool hasExternal = false;
				if (execCallback is not null) {
					if (execCallback(setPattern) is { } callback) {
						hasExternal = true;
						externalSets.Add(setPattern.ToString(), () => callback(setPattern));
					}
				}

				if (!hasExternal) {
					Console.Error.WriteLine($"No set matches '{setPattern}'");
					error = true;
				}
			}
		}

		if (error) {
			Console.Error.WriteLine("Valid Sets:");
			foreach (var set in allBenchmarkTypes) {
				Console.Error.WriteLine($"  {set.Name}");
			}
			Environment.Exit(-2);
		}

		if (CurrentOptions.Clear) {
			Console.Clear();
		}

		foreach (var externalSet in externalSets) {
			externalSet.Value.Invoke();
		}

		foreach (var optionPermutation in optionPermutations) {
			foreach (var benchmarkType in matchingBenchmarkTypes) {
				_ = ConditionalRun(benchmarkType, args, options, optionPermutation.GCType, optionPermutation.Runtime);
			}
		}

		return 0;
	}
}