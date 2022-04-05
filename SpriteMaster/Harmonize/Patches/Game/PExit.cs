using SpriteMaster.Configuration;
using System.Diagnostics;

namespace SpriteMaster.Harmonize.Patches.Game;

static class PExit {
	[Harmonize(
		typeof(StardewValley.InstanceGame),
		"Exit",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		critical: false
	)]
	public static bool Exit() {
		if (!Config.Enabled || !Config.Extras.FastQuit) {
			return true;
		}

		Process.GetCurrentProcess().Kill();

		return false;
	}
}
