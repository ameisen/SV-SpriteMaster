using StardewValley;
using System.Collections.Generic;
using System.Threading;

namespace SpriteMaster.Harmonize.Patches.Game;

class MasterSchedulePatch {
	private static readonly ThreadLocal<HashSet<string>> MasterScheduleSet = new();
	private static readonly ThreadLocal<int> MasterScheduleDepth = new();

	[Harmonize(
	typeof(StardewValley.NPC),
	"parseMasterSchedule",
	Harmonize.Fixation.Prefix,
	Harmonize.PriorityLevel.Last,
	critical: false
)]
	public static bool ParseMasterSchedulePre(StardewValley.NPC __instance, ref Dictionary<int, SchedulePathDescription>? __result, string rawData) {
		if (!Config.Enabled || !Config.Extras.FixMasterSchedule) {
			return true;
		}

		if (__instance is StardewValley.Monsters.Monster) {
			__result = null;
			return false;
		}

		if (!MasterScheduleSet.IsValueCreated) {
			MasterScheduleSet.Value = new() { rawData };
		}
		else if (!MasterScheduleSet.Value!.Add(rawData)) {
			__result = null;
			return false;
		}

		if (!MasterScheduleDepth.IsValueCreated) {
			MasterScheduleDepth.Value = 1;
		}
		else {
			++MasterScheduleDepth.Value;
		}

		return true;
	}

	[Harmonize(
		typeof(StardewValley.NPC),
		"parseMasterSchedule",
		Harmonize.Fixation.Finalizer,
		Harmonize.PriorityLevel.Last,
		critical: false
	)]
	public static void ParseMasterSchedulePost(StardewValley.NPC __instance, string rawData) {
		if (!Config.Enabled || !Config.Extras.FixMasterSchedule) {
			return;
		}

		if (!MasterScheduleDepth.IsValueCreated) {
			MasterScheduleDepth.Value = 0;
		}
		else {
			--MasterScheduleDepth.Value;
		}

		if (MasterScheduleDepth.Value == 0 && MasterScheduleSet.IsValueCreated) {
			MasterScheduleSet.Value!.Clear();
		}
	}

	[Harmonize(
		typeof(StardewValley.NPC),
		"getSchedule",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		critical: false
	)]
	public static bool ParseMasterSchedulePost(StardewValley.NPC __instance, ref Dictionary<int, SchedulePathDescription>? __result, int dayOfMonth) {
		if (!Config.Enabled || !Config.Extras.FixMasterSchedule) {
			return true;
		}

		if (__instance is StardewValley.Monsters.Monster) {
			__result = null;
			return false;
		}

		return true;
	}
}
