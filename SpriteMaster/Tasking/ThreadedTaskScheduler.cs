﻿//#define SM_SINGLE_THREAD

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace SpriteMaster.Tasking;

[DebuggerTypeProxy(typeof(ThreadedTaskSchedulerDebugView))]
[DebuggerDisplay("Id={Id}, ScheduledTasks = {DebugTaskCount}")]
sealed class ThreadedTaskScheduler : TaskScheduler, IDisposable {
	internal static readonly ThreadedTaskScheduler Instance = new();
	internal static readonly TaskFactory TaskFactory = new(Instance);

	private class ThreadedTaskSchedulerDebugView {
		private readonly ThreadedTaskScheduler Scheduler;

		public ThreadedTaskSchedulerDebugView(ThreadedTaskScheduler scheduler) {
			Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
		}

		public IEnumerable<Task> ScheduledTasks => Scheduler.BlockingTaskQueue;
	}

	private readonly CancellationTokenSource DisposeCancellation = new();
	public int ConcurrencyLevel { get; private init; }

	[ThreadStatic]
	private static bool IsTaskProcessingThread = false;

	private readonly Thread[] Threads;
	private readonly BlockingCollection<Task> BlockingTaskQueue = new();

	private int DebugTaskCount => BlockingTaskQueue.Count;

	internal ThreadedTaskScheduler(
		int? concurrencyLevel = null,
		Func<int, string>? threadNameFunction = null,
		bool useForegroundThreads = false,
		ThreadPriority threadPriority = ThreadPriority.Lowest,
		Action<Thread, int>? onThreadInit = null,
		Action<Thread, int>? onThreadFinally = null
	) {
		try {
			if (concurrencyLevel is null or 0) {
#if SM_SINGLE_THREAD
			concurrencyLevel = 1;
#else
				concurrencyLevel = Environment.ProcessorCount;
#endif
			}

			if (concurrencyLevel < 0) {
				throw new ArgumentOutOfRangeException(nameof(concurrencyLevel));
			}

			ConcurrencyLevel = concurrencyLevel.Value;

			Threads = new Thread[ConcurrencyLevel];
			for (int i = 0; i < Threads.Length; ++i) {
				Threads[i] = new(index => DispatchLoop((int)index!, onThreadInit, onThreadFinally)) {
					Priority = threadPriority,
					IsBackground = true,
					Name = threadNameFunction is null ? $"ThreadedTaskScheduler Thread {i}" : threadNameFunction(i),
				};
				try {
					Threads[i].SetApartmentState(ApartmentState.MTA);
				}
				catch (PlatformNotSupportedException) {
					/* do nothing */
				}
			}

			for (int i = 0; i < Threads.Length; ++i) {
				Threads[i].Start(i);
			}
		}
		catch (Exception ex) {
			Debug.Error("Failed to create ThreadedTaskScheduler", ex);
			throw;
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private void DispatchLoop(int index, Action<Thread, int>? onInit, Action<Thread, int>? onFinally) {
		try {
			IsTaskProcessingThread = true;
			var thread = Thread.CurrentThread;
			onInit?.Invoke(thread, index);
			try {
				try {
					while (!Config.ForcedDisable) {
						try {
							foreach (var task in BlockingTaskQueue.GetConsumingEnumerable(DisposeCancellation.Token)) {
								if (task is not null) {
									if (TryExecuteTask(task) || task.IsCompleted) {
										task.Dispose();
									}
								}
							}
						}
						catch (ThreadAbortException) {
							if (!Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload()) {
								Thread.ResetAbort();
							}
						}
					}
				}
				catch (OperationCanceledException) { /* do nothing */ }
			}
			finally {
				onFinally?.Invoke(thread, index);
			}
		}
		catch (Exception ex) {
			Debug.Error("Error in ThreadedTaskScheduler.DispatchLoop", ex);
			throw;
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	protected override void QueueTask(Task task) {
		if (!Config.IsEnabled) {
			return;
		}

		if (DisposeCancellation.IsCancellationRequested) {
			throw new ObjectDisposedException(GetType().Name);
		}

		BlockingTaskQueue.Add(task);
	}

	protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => IsTaskProcessingThread && TryExecuteTask(task);

	protected override IEnumerable<Task> GetScheduledTasks() => BlockingTaskQueue;

	public void Dispose() => DisposeCancellation.Cancel();
}