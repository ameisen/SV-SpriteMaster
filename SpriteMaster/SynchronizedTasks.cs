using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;
using ActionList = System.Collections.Generic.List<System.Action>;
using LoadList = System.Collections.Generic.List<SpriteMaster.TextureAction>;

namespace SpriteMaster {
	internal static class SynchronizedTasks {
		private static DoubleBuffer<ActionList> PendingActions = Config.AsyncScaling.Enabled ? new DoubleBuffer<ActionList>() : null;
		private static DoubleBuffer<LoadList> PendingLoads = Config.AsyncScaling.Enabled ? new DoubleBuffer<LoadList>() : null;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void AddPendingAction (in Action action) {
			lock (PendingActions) {
				PendingActions.Current.Add(action);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void AddPendingLoad (in Action action, int texels) {
			lock (PendingLoads) {
				PendingLoads.Current.Add(new TextureAction(action, texels));
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
				ActionList pendingActions = null;
				lock (PendingActions) {
					var currentPendingActions = PendingActions.Current;
					if (currentPendingActions.Count > 0) {
						pendingActions = currentPendingActions;
						PendingActions.Swap();
					}
				}

				if (pendingActions != null) {
					foreach (var action in pendingActions) {
						action.Invoke();
					}
					pendingActions.Clear();
				}
			}

			if (Config.AsyncScaling.Enabled) {
				LoadList pendingLoads = null;
				lock (PendingLoads) {
					var currentPendingLoads = PendingLoads.Current;
					if (currentPendingLoads.Count > 0) {
						pendingLoads = currentPendingLoads;
						PendingLoads.Swap();
					}
				}

				if (pendingLoads != null) {
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

						if (processed < pendingLoads.Count) {
							lock (PendingLoads) {
								PendingLoads.Current.AddRange(pendingLoads.GetRange(processed, pendingLoads.Count - processed));
							}
						}
						pendingLoads.Clear();
					}
					else {
						foreach (var action in pendingLoads) {
							action.Invoke();
						}
						pendingLoads.Clear();
					}
				}
			}
		}
	}
}
