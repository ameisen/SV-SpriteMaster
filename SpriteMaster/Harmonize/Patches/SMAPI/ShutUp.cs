#if !SHIPPING

using StardewModdingAPI;
using System;

namespace SpriteMaster.Harmonize.Patches.SMAPI;

static class ShutUp {
	[Harmonize(
		typeof(StardewModdingAPI.Framework.ModLoading.RewriteFacades.AccessToolsFacade),
		"StardewModdingAPI.Framework.Monitor",
		"LogImpl",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		critical: false
	)]
	static public bool LogImplPre(IMonitor __instance, string? source, string? message, object level) {
		if (level.ToString() != "Trace") {
			return true;
		}

		switch (source) {
			case "Farm Type Manager":
			case "Quest Framework":
			case "AntiSocial NPCs":
			case "SMAPI":
			case "Json Assets":
			case "Content Patcher":
				return false;
			default:
				return true;
		}
	}
}

#endif
