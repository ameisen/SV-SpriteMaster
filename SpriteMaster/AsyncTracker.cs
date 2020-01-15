using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SpriteMaster {
	internal sealed class AsyncTracker : IDisposable {
		private const bool Enabled = false;

#if DEBUG
		private static readonly object TrackerLock = Enabled ? new object() : null;
		private static readonly HashSet<AsyncTracker> Trackers = Enabled ? new HashSet<AsyncTracker>() : null;

		private readonly string Name;
#endif

		[Conditional("DEBUG")]
		private static void DumpTrackers() {
#if DEBUG
			if (!Enabled) return;

			if (Trackers.Count == 0) {
				Debug.TraceLn("No Asynchronous Tasks In Flight");
				return;
			}

			var output = $"Current Asynchronous Tasks [{Trackers.Count}]:\n";
			foreach (var tracker in Trackers) {
				output += $"\t{tracker.Name}\n";
			}
			output.TrimEnd('\n');
			Debug.TraceLn(output);
#endif
		}

		public AsyncTracker (string name) {
			if (!Enabled) return;

#if DEBUG
			Name = name;
			lock (TrackerLock) {
				Trackers.Add(this);
				DumpTrackers();
			}
#endif
		}

		public void Dispose () {
			if (!Enabled) return;

#if DEBUG
			lock (TrackerLock) {
				Trackers.Remove(this);
				DumpTrackers();
			}
#endif
		}
	}
}
