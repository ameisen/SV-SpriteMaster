using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace SpriteMaster.Types;

sealed class ConcurrentConsumer<T> {
	internal delegate void CallbackDelegate(in T item);

	private readonly Thread ConsumerThread;
	private readonly AutoResetEvent Event = new(false);
	private readonly ConcurrentQueue<T> DataQueue = new();
	private readonly CallbackDelegate Callback;
	private readonly string Name;

	internal ConcurrentConsumer(string name, CallbackDelegate callback) {
		Name = name;
		Callback = callback;

		ConsumerThread = new(Loop) {
			Name = $"ConcurrentConsumer '{name}' Thread",
			Priority = ThreadPriority.BelowNormal,
			IsBackground = true
		};
	}

	internal void Push(in T instance) {
		DataQueue.Enqueue(instance);
		Event.Set();
	}

	private void Loop() {
		var dequeued = new List<T>();

		while (true) {
			dequeued.Clear();

			try {
				Event.WaitOne();
				bool noGC = GC.TryStartNoGCRegion(0x4000);
				try {
					while (DataQueue.TryDequeue(out var data)) {
						try {
							dequeued.Add(data);
						}
						catch (Exception ex) {
							Debug.Error($"Exception during ConcurrentConsumer '{Name}' Loop", ex);
						}
					}
				}
				finally {
					if (noGC) {
						try {
							GC.EndNoGCRegion();
						}
						catch { }
					}
				}

				foreach (var item in dequeued) {
					try {
						Callback(item);
					}
					catch (Exception ex) {
						Debug.Error($"Exception during ConcurrentConsumer '{Name}' Loop", ex);
					}
				}
			}
			catch (ThreadAbortException) {
				break;
			}
			catch (ObjectDisposedException) {
				break;
			}
			catch (AbandonedMutexException) {
				continue;
			}
		}
	}
}
