using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using ActionList = System.Collections.Generic.List<System.Action>;
using LoadList = System.Collections.Generic.List<SpriteMaster.TextureAction>;

namespace SpriteMaster {
	internal static class SynchronizedTasks {
		private static DoubleBuffer<ActionList> PendingActions = Config.AsyncScaling.Enabled ? new DoubleBuffer<ActionList>() : null;
		private static DoubleBuffer<LoadList> PendingLoads = Config.AsyncScaling.Enabled ? new DoubleBuffer<LoadList>() : null;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void AddPendingAction (in Action action) {
			var current = PendingActions.Current;
			lock (current) {
				current.Add(action);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void AddPendingLoad (in Action action, int texels) {
			var current = PendingLoads.Current;
			lock (current) {
				current.Add(new TextureAction(action, texels));
			}
		}

		private static double DurationPerTexel = 0.0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void AddDuration(TextureAction action, in TimeSpan duration) {
			// This isn't a true running average - we'd lose too much precision over time when the sample count got too high, and I'm lazy.

			if (action.Texels == 0) {
				return;
			}

			DurationPerTexel += (double)duration.Ticks / action.Texels;
			DurationPerTexel *= 0.5;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static TimeSpan EstimateDuration(this TextureAction action) {
			return new TimeSpan((DurationPerTexel * action.Texels).NextInt());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void ProcessPendingActions (in TimeSpan remainingTime) {
			var startTime = DateTime.Now;
			{
				var pendingActions = PendingActions.Current;
				bool invoke;
				lock (pendingActions) {
					invoke = pendingActions.Any();
				}

				if (invoke) {
					PendingActions.SwapAtomic();
					lock (pendingActions) {
						foreach (var action in pendingActions) {
							action.Invoke();
						}
						pendingActions.Clear();
					}
				}
			}

			if (Config.AsyncScaling.Enabled) {
				var pendingLoads = PendingLoads.Current;
				bool invoke;
				lock (pendingLoads) {
					invoke = pendingLoads.Any();
				}

				if (invoke) {
					PendingLoads.SwapAtomic();
					lock (pendingLoads) {
						if (Config.AsyncScaling.ThrottledSynchronousLoads) {
							int processed = 0;
							foreach (var action in pendingLoads) {
								var estimate = action.EstimateDuration();
								if (processed > 0 && (DateTime.Now - startTime) + estimate > remainingTime) {
									break;
								}

								var start = DateTime.Now;
								action.Invoke();
								var duration = DateTime.Now - start;
								AddDuration(action, duration);

								++processed;
							}

							// TODO : I'm not sure if this is necessary. The next frame, it'll probably come back around again to this buffer.
							if (processed < pendingLoads.Count) {
								var current = PendingLoads.Current;
								lock (current) {
									current.AddRange(pendingLoads.GetRange(processed, pendingLoads.Count - processed));
								}
							}
						}
						else {
							foreach (var action in pendingLoads) {
								action.Invoke();
							}
						}
						pendingLoads.Clear();
					}
				}
			}
		}
	}
}
