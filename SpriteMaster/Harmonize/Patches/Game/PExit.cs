using System.Diagnostics;

namespace SpriteMaster.Harmonize.Patches.Game;

internal static class PExit {
	[Harmonize(
		typeof(StardewValley.InstanceGame),
		"Exit",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		critical: false
	)]
	public static bool Exit() {
		if (!SMConfig.IsUnconditionallyEnabled || !SMConfig.Extras.FastQuit) {
			return true;
		}

		Process.GetCurrentProcess().Kill();

		return false;
	}
}
