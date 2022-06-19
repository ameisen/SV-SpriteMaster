using StardewModdingAPI;
using StardewValley;
using System.IO;
using static SpriteMaster.Harmonize.Harmonize;

namespace SpriteMaster.Harmonize.Patches;

internal static class ModContentManager {
	[HarmonizeSmapiVersionConditional(Comparator.GreaterThanOrEqual, "3.15.0")]
	[Harmonize(
		"StardewModdingAPI.Framework.ContentManagers.ModContentManager",
		"LoadRawImageData",
		Fixation.Prefix,
		PriorityLevel.Last,
		instance: true
	)]
	public static bool OnLoadRawImageData(
		LocalizedContentManager __instance, ref IRawTextureData __result, FileInfo file, bool forRawData
	) {
		return Caching.TextureFileCache.OnLoadRawImageData(__instance, ref __result, file, forRawData);
	}
}