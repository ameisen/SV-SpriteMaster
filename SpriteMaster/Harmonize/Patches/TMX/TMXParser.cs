using System;
using System.Collections.Concurrent;
using System.IO;
using System.Xml;

namespace SpriteMaster.Harmonize.Patches.TMX;

internal static class TMXParser {
	#region By Path

	private static readonly ConcurrentDictionary<string, (DateTime Modified, object Result)> ParseFileCache = new();

	[Harmonize(
		"TMXTile.TMXParser",
		"Parse",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last
	)]
	public static bool ParsePre(object __instance, ref object __result, string path) {
		if (!ParseFileCache.TryGetValue(path, out var cachedResult)) {
			return true;
		}

		var currentTime = File.GetLastWriteTimeUtc(path);
		if (cachedResult.Modified != currentTime) {
			return true;
		}

		__result = cachedResult.Result;
		return false;

	}

	[Harmonize(
		"TMXTile.TMXParser",
		"Parse",
		Harmonize.Fixation.Postfix,
		Harmonize.PriorityLevel.Last
	)]
	public static void ParsePost(object __instance, object __result, string path) {
		var currentTime = File.GetLastWriteTimeUtc(path);

		ParseFileCache.AddOrUpdate(path, _ => (currentTime, __result), (_, _) => (currentTime, __result));
	}

	#endregion

	/*
	#region By XML Reader

	[Harmonize(
		"TMXTile.TMXParser",
		"Parse",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last
	)]
	public static bool ParsePre(object __instance, ref object __result, XmlReader reader) {
		reader.ReadContentAsString();
		if (!ParseFileCache.TryGetValue(path, out var cachedResult)) {
			return true;
		}

		var currentTime = File.GetLastWriteTimeUtc(path);
		if (cachedResult.Modified != currentTime) {
			return true;
		}

		__result = cachedResult.Result;
		return false;

	}

	[Harmonize(
		"TMXTile.TMXParser",
		"Parse",
		Harmonize.Fixation.Postfix,
		Harmonize.PriorityLevel.Last
	)]
	public static void ParsePost(object __instance, object __result, XmlReader reader path) {
		var currentTime = File.GetLastWriteTimeUtc(path);

		ParseFileCache.AddOrUpdate(path, _ => (currentTime, __result), (_, _) => (currentTime, __result));
	}

	#endregion
	*/
}
