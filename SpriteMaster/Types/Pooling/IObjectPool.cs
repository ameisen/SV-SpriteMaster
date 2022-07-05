using System.Runtime.CompilerServices;

namespace SpriteMaster.Types.Pooling;

internal interface IObjectPool<T> where T : class, new() {
	int Count { get; }
	long Allocated { get; }

	T Get();

	IPooledObject<T> GetSafe();

	void Return(T value);
}

internal interface ISealedObjectPool<T, TPool> : IObjectPool<T> where T : class, new() where TPool : ISealedObjectPool<T, TPool> {
	IPooledObject<T> IObjectPool<T>.GetSafe() => GetSafe();

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal new sealed PooledObject<T, TPool> GetSafe() {
		return new(Get(), this);
	}
}
