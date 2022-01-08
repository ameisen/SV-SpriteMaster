using SpriteMaster.Tasking;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SpriteMaster;

static class ThreadQueue {
	private static readonly ThreadedTaskScheduler TaskScheduler = new();
	private static readonly TaskFactory TaskFactory = new(TaskScheduler);

	internal delegate void QueueFunctor<T>(T state) where T : class;

	private readonly struct Functor {
		internal readonly Action<object> Callback;
		internal readonly object State;

		[MethodImpl(Runtime.MethodImpl.Hot)]
		private Functor(Action<object> callback, object state) {
			Callback = callback;
			State = state;
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		internal static Functor Of(Action<object> callback, object state) => new(callback, state);

		[MethodImpl(Runtime.MethodImpl.Hot)]
		internal static Functor Of<T>(QueueFunctor<T> callback, T state) where T : class => new(o => callback((T)o), state);

		[MethodImpl(Runtime.MethodImpl.Hot)]
		internal static Functor Of(Action<object> callback) => new(callback, null);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static void Enqueue(in Functor functor) {
		TaskFactory.StartNew(functor.Callback, functor.State);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Queue<T>(QueueFunctor<T> functor, T argument) where T : class => Enqueue(Functor.Of(functor, argument));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Queue(Action<object> functor, object argument) => Enqueue(Functor.Of(functor, argument));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Queue(Action<object> functor) => Enqueue(Functor.Of(functor));
}
