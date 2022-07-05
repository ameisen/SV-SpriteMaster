//#define VALIDATE_ROUTES

using LinqFasterer;
using SpriteMaster.Configuration;
using SpriteMaster.Extensions;
using SpriteMaster.Extensions.Reflection;
using SpriteMaster.Types.Reflection;
using StardewValley;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SpriteMaster.Harmonize.Patches.Game.Pathfinding;

internal static partial class Pathfinding {
	private static readonly VariableInfo? RoutesFromLocationToLocationInfo = typeof(NPC).GetStaticVariable("routesFromLocationToLocation");
	private static readonly VariableStaticAccessor<List<List<string>>>? RoutesFromLocationToLocation = RoutesFromLocationToLocationInfo?.GetStaticAccessor<List<List<string>>>();

	private static bool RoutesFromLocationToLocationSet(ConcurrentBag<List<string>> routes) {
		if (RoutesFromLocationToLocation is not {} accessor) {
			return false;
		}

		lock (PathLock) {
			accessor.Value = routes.ToList();
			return true;
		}
	}

	static Pathfinding() {
		if (RoutesFromLocationToLocation is null) {
			Debug.Warning("Could not find 'NPC.routesFromLocationToLocation'");
		}
	}

	[Harmonize(
		typeof(NPC),
		"getLocationRoute",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		critical: false
	)]
	public static bool GetLocationRoute(NPC __instance, ref List<string>? __result, string startingLocation, string endingLocation) {
		if (!Config.IsUnconditionallyEnabled || !Config.Extras.Pathfinding.OptimizeWarpPoints) {
			return true;
		}

		if (__instance is StardewValley.Monsters.Monster) {
			__result = null;
			return false;
		}

		// TODO : Handle the MensLocker/WomensLocker overrides. We effectively will need two more route maps (or one if gender can never be 'neither').
		// && ((int)this.gender == 0 || !s.Contains<string>("BathHouse_MensLocker", StringComparer.Ordinal)) && ((int)this.gender != 0 || !s.Contains<string>("BathHouse_WomensLocker", StringComparer.Ordinal))

		if (FasterRouteMap.TryGetValue(startingLocation, out var innerRoute)) {
			if (innerRoute.TryGetValue(endingLocation, out var route) && route.Count != 0) {
				__result = route;
				return false;
			}
		}

		return true;
	}

#if VALIDATE_ROUTES
	[Harmonize(
		typeof(NPC),
		"populateRoutesFromLocationToLocationList",
		Harmonize.Fixation.Reverse,
		instance: false,
		critical: false
	)]
	public static void PopulateRoutesFromLocationToLocationListReverse() {
		ThrowHelper.ThrowReversePatchException();
	}

	private static bool RealPopulate = false;
#endif



	[Conditional("VALIDATE_ROUTES")]
	private static void ValidateRoutes(List<List<string>> referenceRoutes, List<List<string>> calculatedRoutes) {
		Dictionary<(string Start, string End), List<string>> referenceRoutesMap = new(referenceRoutes.Count);
		foreach (var route in referenceRoutes) {
			if (!referenceRoutesMap.TryAdd((route.FirstF(), route.LastF()), route)) {
				if (route.Count < referenceRoutesMap[(route.FirstF(), route.LastF())].Count) {
					referenceRoutesMap[(route.FirstF(), route.LastF())] = route;
				}
			}
		}

		var duplicateRoutes = new HashSet<(string Start, string End)>();
		var mismatchedRoutes = new Dictionary<(string Start, string End), (List<string> Reference, List<string> Calculated)>();


		Dictionary<(string Start, string End), List<string>> calculatedRoutesMap = new(calculatedRoutes.Count);
		foreach (var route in calculatedRoutes) {
			if (!calculatedRoutesMap.TryAdd((route.FirstF(), route.LastF()), route)) {
				duplicateRoutes.Add((route.FirstF(), route.LastF()));
			}
		}

		if (referenceRoutesMap.Count != calculatedRoutesMap.Count) {
			Debug.Error($"Route Counts mismatch");
		}
		foreach (var routePair in referenceRoutesMap) {
			if (!calculatedRoutesMap.TryGetValue(routePair.Key, out var calculatedRoute)) {
				Debug.Error($"Calculated Routes Map missing '{routePair.Key}'");
				continue;
			}

			if (!routePair.Value.SequenceEqualF(calculatedRoute)) {
				mismatchedRoutes.Add(routePair.Key, (routePair.Value, calculatedRoute));
			}
		}
		foreach (var routePair in calculatedRoutesMap) {
			if (!referenceRoutesMap.TryGetValue(routePair.Key, out var referenceRoute)) {
				Debug.Error($"Reference Routes Map missing '{routePair.Key}'");
				continue;
			}

			if (!routePair.Value.SequenceEqualF(referenceRoute)) {
				mismatchedRoutes.Add(routePair.Key, (referenceRoute, routePair.Value));
			}
		}

		if (duplicateRoutes.Count != 0) {
			Debug.Error("Duplicate Routes:");
			foreach (var route in duplicateRoutes) {
				Debug.Error($"Duplicate Route {(route.Start, route.End)}");
			}
		}

		if (mismatchedRoutes.Count != 0) {
			Debug.Error("Mismatched Routes:");
			foreach (var (key, routes) in mismatchedRoutes) {
				Debug.Error($"Route '{key}' mismatch:\n  {string.Join(':', routes.Reference)}\n  {string.Join(':', routes.Calculated)}");
			}
		}
	}

	[Harmonize(
		typeof(NPC),
		"populateRoutesFromLocationToLocationList",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false,
		critical: false
	)]
	public static bool PopulateRoutesFromLocationToLocationList() {
		if (!Config.IsUnconditionallyEnabled || !Config.Extras.Pathfinding.OptimizeWarpPoints) {
			return true;
		}

#if VALIDATE_ROUTES
		RealPopulate = true;
		try {
			PopulateRoutesFromLocationToLocationListReverse();
		}
		finally {
			RealPopulate = false;
		}

		var referenceRoutes = RoutesFromLocationToLocation!?.Value!;
#endif

		var routeList = new ConcurrentBag<List<string>>();

		var locations = new Dictionary<string, GameLocation>(Game1.locations.WhereF(location => location is not null).SelectF(location => new KeyValuePair<string, GameLocation>(location.Name, location)));

		GameLocation? backwoodsLocation = Game1.locations.FirstOrDefaultF(location => location.Name == "Backwoods");

		// Iterate over every location in parallel, and collect all paths to every other location.
		Parallel.ForEach(Game1.locations, location => {
			if (location is not Farm && !ReferenceEquals(location, backwoodsLocation)) {
				var route = new List<string>();
				ExploreWarpPointsImpl(location, route, routeList, locations);
			}
		});

		// Set the RoutesFromLocationToLocation list, and also generate a faster 'FasterRouteMap' to perform path lookups.
		RoutesFromLocationToLocationSet(routeList);
		FasterRouteMap.Clear();
		foreach (var route in routeList) {
			var innerRoutes = FasterRouteMap.GetOrAddDefault(route.FirstF(), () => new());
			innerRoutes[route.LastF()] = route;
		}

#if VALIDATE_ROUTES
		ValidateRoutes(referenceRoutes, routeList.ToList());
#endif

		return false;
	}

	[Harmonize(
		typeof(NPC),
		"exploreWarpPoints",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false,
		critical: false
	)]
	public static bool ExploreWarpPoints(ref bool __result, GameLocation l, List<string> route) {
		if (!Config.IsUnconditionallyEnabled || !Config.Extras.Pathfinding.OptimizeWarpPoints) {
			return true;
		}

#if VALIDATE_ROUTES
		if (RealPopulate) {
			return true;
		}
#endif

		// RoutesFromLocationToLocation is always a new list when first entering this method
		var routeList = new ConcurrentBag<List<string>>();

		var locations = new Dictionary<string, GameLocation>(Game1.locations.WhereF(location => location is not null).SelectF(location => new KeyValuePair<string, GameLocation>(location.Name, location)));

		// Single location pathing search.
		__result = ExploreWarpPointsImpl(l, route, routeList, locations);

		RoutesFromLocationToLocationSet(routeList);
		FasterRouteMap.Clear();
		foreach (var listedRoute in routeList) {
			var innerRoutes = FasterRouteMap.GetOrAddDefault(listedRoute.FirstF(), () => new());
			innerRoutes[listedRoute.LastF()] = listedRoute;
		}
		return false;
	}
}
