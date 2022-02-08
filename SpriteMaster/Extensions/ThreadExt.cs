using System.Threading;

namespace SpriteMaster.Extensions;

static class ThreadExt {
	internal static Thread Run(ThreadStart start, bool background = false) {
		var thread = new Thread(start) {
			IsBackground = background
		};
		thread.Start();
		return thread;
	}

	internal static Thread Run(ParameterizedThreadStart start, object obj, bool background = false) {
		var thread = new Thread(start) {
			IsBackground = background
		};
		thread.Start(obj);
		return thread;
	}
}
