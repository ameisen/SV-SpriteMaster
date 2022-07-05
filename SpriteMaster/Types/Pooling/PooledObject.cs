using System;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SpriteMaster.Types.Pooling;

internal interface IPooledObject<T> : IDisposable where T : class, new() {
	T Value { get; }
}

internal interface ISealedPooledObject<T, TPooledObject> : IPooledObject<T> where T : class, new() where TPooledObject : ISealedPooledObject<T, TPooledObject> {
	T IPooledObject<T>.Value => Value;

	[MethodImpl(Runtime.MethodImpl.Inline)]
	protected void OnDispose(T value);

	[MethodImpl(Runtime.MethodImpl.Inline)]
	void IDisposable.Dispose() {
#if DEBUG || DEVELOPMENT
			if (Value is not {} value) {
				ThrowHelper.ThrowInvalidOperationException($"{nameof(ISealedPooledObject<T, TPooledObject>)}.{nameof(Value)} was already disposed!");
				return;
			}
#else
		var value = Value;
#endif

		OnDispose(value);
	}
}

[StructLayout(LayoutKind.Auto)]
internal readonly struct PooledObject<T, TPool> : ISealedPooledObject<T, PooledObject<T, TPool>> where T : class, new() where TPool : IObjectPool<T> {
	public readonly T Value { get; }
	private readonly IObjectPool<T> Pool;

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal PooledObject(T value, IObjectPool<T> pool) {
		Value = value;
		Pool = pool;
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	readonly void ISealedPooledObject<T, PooledObject<T, TPool>>.OnDispose(T value) {
		Pool.Return(value);

#if DEBUG || DEVELOPMENT
			Unsafe.AsRef(Value) = null!;
#endif
	}
}

[StructLayout(LayoutKind.Auto)]
internal readonly struct DefaultPooledObject<T> : ISealedPooledObject<T, DefaultPooledObject<T>> where T : class, new() {
	public readonly T Value { get; }
	private static ObjectPool<T> Pool => ObjectPool<T>.Default;

	[MethodImpl(Runtime.MethodImpl.Inline)]
	internal DefaultPooledObject(T value) {
		Value = value;
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	readonly void ISealedPooledObject<T, DefaultPooledObject<T>>.OnDispose(T value) {
		Pool.Return(value);

#if DEBUG || DEVELOPMENT
			Unsafe.AsRef(Value) = null!;
#endif
	}
}

