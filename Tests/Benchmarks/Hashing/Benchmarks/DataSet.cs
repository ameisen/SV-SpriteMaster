using Microsoft.Toolkit.HighPerformance;
using System.Numerics;

namespace Hashing.Benchmarks;

public interface IDataSet<T> {
	public T Data { get; }
}

public unsafe interface IDataSetPtr<T> : IDataSet<T[]> where T : unmanaged {
	public T* DataPtr { get; }
}

public readonly struct DataSetArray<T> : IDataSetPtr<T> where T : unmanaged {
	public readonly T[] Data { get; }
	public readonly unsafe T* DataPtr { get; }

	private readonly uint Index => (Data.Length == 0) ? 0u : (uint)BitOperations.Log2((uint)Data.Length) + 1u;

	private static T[] MakeArray(Random random, long length) {
		var data = GC.AllocateUninitializedArray<T>((int)length, pinned: true);
		var span = data.AsSpan().AsBytes();
		random.NextBytes(span);

		return data;
	}

	public DataSetArray(Random random, long length) :
		this(MakeArray(random, length)) {
	}

	public DataSetArray(T[] data) {
		Data = data;
		unsafe {
			fixed (T* ptr = data) {
				DataPtr = ptr;
			}
		}

		//Validate(this);
	}

	public override readonly string ToString() => $"({Index:D2}) {Data.Length}";
}

public readonly struct DataSet<T> : IDataSet<T> where T : notnull {
	public readonly T Data { get; }

	private readonly uint Index {
		get {
			if (Data is string str) {
				return (str.Length == 0) ? 0u : (uint)BitOperations.Log2((uint)str.Length) + 1u;
			}

			return (uint)Data.GetHashCode();
		}
	}

	private readonly string Name {
		get {
			if (Data is string str) {
				return str.Length.ToString();
			}

			return Data.ToString() ?? "unknown";
		}
	}

	public DataSet(T data) {
		Data = data;
	}

	public override readonly string ToString() => $"({Index:D2}) {Name}";
}
