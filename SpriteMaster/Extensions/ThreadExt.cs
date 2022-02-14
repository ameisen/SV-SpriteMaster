using System.Threading;

namespace SpriteMaster.Extensions;

static class ThreadExt {
	internal static Thread Run(ThreadStart start, bool background = false, string? name = null) {
		var thread = new Thread(start) {
			IsBackground = background,
			Name = name
		};
		thread.Start();
		return thread;
	}

	internal static Thread Run(ParameterizedThreadStart start, object obj, bool background = false, string? name = null) {
		var thread = new Thread(start) {
			IsBackground = background,
			Name = name
		};
		thread.Start(obj);
		return thread;
	}
}
