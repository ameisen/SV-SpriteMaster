﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpriteMaster.Harmonize.Patches.TMX;

/*
internal static class TMXFormat {

	private static readonly ConcurrentDictionary<object, object> MapCache = new();

	[Harmonize(
		"TMXTile.TMXFormat",
		"Load",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last
	)]
	public static bool LoadPre(object __instance, ref object __result, object tmxMap) {
		if (!MapCache.TryGetValue(tmxMap, out var cachedResult)) {
			return true;
		}

		__result = cachedResult;
		return false;

	}

	[Harmonize(
		"TMXTile.TMXFormat",
		"Load",
		Harmonize.Fixation.Postfix,
		Harmonize.PriorityLevel.Last
	)]
	public static void LoadPost(object __instance, object __result, object tmxMap) {
		_ = MapCache.TryAdd(tmxMap, __result);
	}

}
*/
