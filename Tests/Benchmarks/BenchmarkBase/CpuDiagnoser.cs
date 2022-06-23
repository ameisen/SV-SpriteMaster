using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using JetBrains.Annotations;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Benchmarks.BenchmarkBase;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class)]
public class CpuDiagnoserAttribute : Attribute, IConfigSource
{
	public IConfig Config { get; }

	public CpuDiagnoserAttribute()
	{
		Config = ManualConfig.CreateEmpty().AddDiagnoser(new CpuDiagnoser());
	}
}

[PublicAPI]
public class CpuDiagnoser : IDiagnoser {
	private const string DiagnoserId = nameof(CpuDiagnoser);
	public static readonly CpuDiagnoser Default = new();

	private readonly Process Process;

	private struct TimeSpanPair {
		internal TimeSpan Start;
		internal TimeSpan End;

		internal TimeSpan Duration => End - Start;
	}

	private TimeSpanPair User;
	private TimeSpanPair Privileged;
	private TimeSpanPair Total;

	public CpuDiagnoser()
	{
		Process = Process.GetCurrentProcess();
	}

	public IEnumerable<string> Ids => new[] { nameof(CpuDiagnoser) };

	public IEnumerable<IExporter> Exporters => Array.Empty<IExporter>();

	public IEnumerable<IAnalyser> Analysers => Array.Empty<IAnalyser>();

	public void DisplayResults(ILogger logger)
	{
	}

	public RunMode GetRunMode(BenchmarkCase benchmarkCase) => RunMode.NoOverhead;

	public void Handle(HostSignal signal, DiagnoserActionParameters parameters) {
		switch (signal)
		{
			case HostSignal.BeforeActualRun:
				User.Start = Process.UserProcessorTime;
				Privileged.Start = Process.PrivilegedProcessorTime;
				Total.Start = Process.TotalProcessorTime;
				break;
			case HostSignal.AfterActualRun:
				User.End = Process.UserProcessorTime;
				Privileged.End = Process.PrivilegedProcessorTime;
				Total.End = Process.TotalProcessorTime;
				break;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double GetMetricValue(DiagnoserResults results, in TimeSpan duration) {
		return duration.Ticks * 100.0 / results.TotalOperations;
	}

	public IEnumerable<Metric> ProcessResults(DiagnoserResults results) {
		yield return new(CpuUserMetricDescriptor.Instance, GetMetricValue(results, User.Duration));
		yield return new(CpuPrivilegedMetricDescriptor.Instance, GetMetricValue(results, Privileged.Duration));
		yield return new(CpuTotalMetricDescriptor.Instance, GetMetricValue(results, Total.Duration));
	}

	public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters) =>
		Array.Empty<ValidationError>();

	private abstract class MetricDescriptor<TMetricDescriptor> : IMetricDescriptor where TMetricDescriptor : MetricDescriptor<TMetricDescriptor>, new() {
		internal static readonly TMetricDescriptor Instance = new();

		public string Id { get; }
		public string DisplayName => Id;
		public string Legend => Id;
		public string NumberFormat => "0.##";
		public UnitType UnitType => UnitType.Time;
		public string Unit => "ns";
		public bool TheGreaterTheBetter => false;
		public int PriorityInCategory => 1;

		protected MetricDescriptor(string id) {
			Id = id;
		}
	}

	private class CpuUserMetricDescriptor : MetricDescriptor<CpuUserMetricDescriptor> {
		public CpuUserMetricDescriptor() : base("CPU User Time") { }
	}

	private class CpuPrivilegedMetricDescriptor : MetricDescriptor<CpuPrivilegedMetricDescriptor> {
		public CpuPrivilegedMetricDescriptor() : base("CPU Privileged Time") { }
	}

	private class CpuTotalMetricDescriptor : MetricDescriptor<CpuTotalMetricDescriptor> {
		public CpuTotalMetricDescriptor() : base("CPU Total Time") { }
	}
}