using SpriteMaster.Types.Pooling;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions;

internal static class ObjectPoolExt {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static DefaultPooledObject<T> Take<T>() where T : class, new() =>
		ObjectPoolExt<T>.Take();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static PooledObject<T, TrackingObjectPool<T>> TakeTracked<T>() where T : class, new() =>
		ObjectPoolExt<T>.TakeTracked();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static T Get<T>() where T : class, new() =>
		ObjectPoolExt<T>.Get();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static T GetTracked<T>() where T : class, new() =>
		ObjectPoolExt<T>.GetTracked();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Return<T>(T value) where T : class, new() =>
		ObjectPoolExt<T>.Return(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ReturnTracked<T>(T value) where T : class, new() =>
		ObjectPoolExt<T>.ReturnTracked(value);
}

internal static class ObjectPoolExt<T> where T : class, new() {
	internal static ObjectPool<T> DefaultPool => ObjectPool<T>.Default;
	internal static TrackingObjectPool<T> DefaultTrackingPool => TrackingObjectPool<T>.Default;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static DefaultPooledObject<T> Take() =>
		new(DefaultPool.Get());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static PooledObject<T, TrackingObjectPool<T>> TakeTracked() =>
		new(DefaultTrackingPool.Get(), DefaultTrackingPool);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static T Get() =>
		DefaultPool.Get();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static T GetTracked() =>
		DefaultTrackingPool.Get();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Return(T value) =>
		DefaultPool.Return(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ReturnTracked(T value) =>
		DefaultTrackingPool.Return(value);
}
