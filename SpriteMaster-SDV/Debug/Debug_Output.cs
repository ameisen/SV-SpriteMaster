using SpriteMaster.Extensions.Reflection;
using StardewModdingAPI;
using System.Reflection;

namespace SpriteMaster;

internal static partial class Debug {
	private static IMonitor? GetTemporaryMonitor() {
		object? sCoreInstance = null;

		if (ReflectionExt.GetTypeExt("StardewModdingAPI.Framework.SCore")?.GetStaticVariable("Instance") is not {} instanceInfo) {
			return null;
		}
		sCoreInstance = instanceInfo.GetValue(null);

		if (ReflectionExt.GetTypeExt("StardewModdingAPI.Framework.Logging.LogManager") is not {} logManagerType) {
			return null;
		}

		if (logManagerType.GetMethod(
					"GetMonitor",
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
					null,
					new [] {typeof(string)},
					null
				) is not { } getMonitorInfo) {
			return null;
		}

		if (sCoreInstance is null || ReflectionExt.GetTypeExt("StardewModdingAPI.Framework.SCore")?.GetInstanceVariable("LogManager") is not {} logManagerInfo) {
			return null;
		}

		if (logManagerInfo.GetValue(sCoreInstance) is not {} logManager) {
			return null;
		};

		try {
			return getMonitorInfo.Invoke(logManagerInfo, new object[] { "SpriteMaster" }) as IMonitor;
		}
		catch {
			return null;
		}
	}

	private static volatile IMonitor? TemporaryMonitor = null;

	private static void WriteToLogImpl(string str, LogLevel level) {
		if (SpriteMaster.Self.Monitor is not {} monitor) {
			if (TemporaryMonitor is not { } tempMonitor) {
				tempMonitor = GetTemporaryMonitor();
			}

			monitor = tempMonitor;
		}
		else {
			TemporaryMonitor = null;
		}

		try {
			if (monitor is not null) {
				monitor.Log(str, level);
				return;
			}
		}
		catch {
			// Swallow Exceptions
		}
	}
}
