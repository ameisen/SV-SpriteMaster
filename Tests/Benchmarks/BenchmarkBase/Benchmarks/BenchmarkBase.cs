using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace Benchmarks.BenchmarkBase.Benchmarks;

[Orderer(SummaryOrderPolicy.Declared, MethodOrderPolicy.Declared)]
[CsvExporter, HtmlExporter]
[MinColumn, MaxColumn]
public abstract class BenchmarkBase {
}

public abstract class BenchmarkBase<TDataType, TBase> : BenchmarkBase where TDataType : IDataSet<TBase> {
	[JetBrains.Annotations.UsedImplicitly]
	public static List<TDataType> DataSets { get; protected set; } = new();
}

public abstract class BenchmarkBaseImpl<TDataType, TBase> : BenchmarkBase<TDataType, TBase> where TDataType : IDataSet<TBase> {
}
