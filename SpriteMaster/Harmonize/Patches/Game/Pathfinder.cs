using Microsoft.Xna.Framework;
using Priority_Queue;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using StardewValley;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static StardewValley.PathFindController;

namespace SpriteMaster.Harmonize.Patches.Game;

static class Pathfinder {
	private static readonly Action<List<List<string>>>? RoutesFromLocationToLocationSet = typeof(NPC).GetFieldSetter<List<List<string>>>("routesFromLocationToLocation");
	private static readonly Dictionary<string, Dictionary<string, List<string>>> FasterRouteMap = new();

	[Harmonize(
		typeof(NPC),
		"getLocationRoute",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last
	)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool GetLocationRoute(NPC __instance, ref List<string>? __result, string startingLocation, string endingLocation) {
		if (!Config.Enabled || !Config.Extras.OptimizeWarpPoints) {
			return true;
		}

		// && ((int)this.gender == 0 || !s.Contains<string>("BathHouse_MensLocker", StringComparer.Ordinal)) && ((int)this.gender != 0 || !s.Contains<string>("BathHouse_WomensLocker", StringComparer.Ordinal))

		if (FasterRouteMap.TryGetValue(startingLocation, out var innerRoute)) {
			if (innerRoute.TryGetValue(endingLocation, out var route) && route is not null && route.Count != 0) {
				__result = route;
				return false;
			}
		}

		return true;
	}

	[Harmonize(
		typeof(Game1),
		"warpCharacter",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false
	)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool WarpCharacter(NPC character, GameLocation targetLocation, XNA.Vector2 position) {
		if (!Config.Enabled || !Config.Extras.AllowNPCsOnFarm || !Config.Extras.OptimizeWarpPoints) {
			return true;
		}

		if (character is null || Game1.player is not Farmer player) {
			return true;
		}

		bool isFamily = false;
		if (player.friendshipData.TryGetValue(character.Name, out var value)) {
			isFamily = value.IsMarried() || value.IsRoommate();
		}
		if (!isFamily && (player.getChildren()?.Contains(character) ?? false)) {
			isFamily = true;
		}

		// https://github.com/aedenthorn/StardewValleyMods/blob/master/FreeLove/FarmerPatches.cs
		if (character == player.getSpouse() || character == player.getPet() || isFamily) {
			var trace = new StackTrace();
			foreach (var frame in trace.GetFrames()) {
				if (frame.GetMethod() is MethodBase method) {
					if (method.DeclaringType == typeof(StardewValley.Event)) {
						return true;
					}
				}
			}

			character.controller = new PathFindController(c: character, location: targetLocation, endPoint: Utility.Vector2ToPoint(position), finalFacingDirection: -1);
			if (character.controller.pathToEndPoint is null) {
				character.controller = null;
				return true;
			}
			return false;
		}

		return true;
	}

	[Harmonize(
		typeof(Game1),
		"warpCharacter",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false
	)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool WarpCharacter(NPC character, string targetLocationName, XNA.Vector2 position) {
		return WarpCharacter(character, Game1.getLocationFromName(targetLocationName), position);
	}

	[Harmonize(
		typeof(Game1),
		"warpCharacter",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false
	)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool WarpCharacter(NPC character, string targetLocationName, XNA.Point position) {
		return WarpCharacter(character, Game1.getLocationFromName(targetLocationName), Utility.PointToVector2(position));
	}

	[Harmonize(
		typeof(GameLocation),
		"characterDestroyObjectWithinRectangle",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last
	)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool CharacterDestroyObjectWithinRectangle(GameLocation __instance, ref bool __result, XNA.Rectangle rect, bool showDestroyedObject) {
		if (__instance.IsFarm || __instance.IsGreenhouse) {
			__result = false;
			return false;
		}
		
		return true;
	}

	[Harmonize(
		typeof(NPC),
		"populateRoutesFromLocationToLocationList",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false
	)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool PopulateRoutesFromLocationToLocationList() {
		if (!Config.Enabled || !Config.Extras.OptimizeWarpPoints) {
			return true;
		}

		if (RoutesFromLocationToLocationSet is null) {
			return true;
		}

		var routeList = new ConcurrentBag<List<string>>();

		var locations = new Dictionary<string, GameLocation?>(Game1.locations.Select(location => new KeyValuePair<string, GameLocation?>(location.Name, location)));

		Parallel.ForEach(Game1.locations, location => {
			if (Config.Extras.AllowNPCsOnFarm || (location is not Farm && location.Name != "Backwoods")) {
				var route = new List<string>();
				ExploreWarpPointsImpl(location, route, routeList, locations);
			}
		});

		RoutesFromLocationToLocationSet(routeList.ToList());
		foreach (var route in routeList) {
			var innerRoutes = FasterRouteMap.GetOrAddDefault(route.First(), () => new Dictionary<string, List<string>>());
			innerRoutes![route.Last()] = route;
		}

		// →

		using var fs = new StreamWriter(@"D:\sdv_paths.txt");
		foreach (var outerRoutes in FasterRouteMap) {
			fs.WriteLine($"{outerRoutes.Key}:");

			int len = 0;
			foreach (var innerRoutes in outerRoutes.Value) {
				len = Math.Max(len, innerRoutes.Key.Length);
			}

			int i = 0;
			foreach (var innerRoutes in outerRoutes.Value) {
				fs.WriteLine($" {(++i == outerRoutes.Value.Count ? '└' : '├')} {innerRoutes.Key.PadRight(len)} :: {string.Join(" → ", innerRoutes.Value)}");
			}
		}

		return false;
	}

	[Harmonize(
		typeof(NPC),
		"exploreWarpPoints",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false
	)]
	[MethodImpl(Runtime.MethodImpl.Hot)]
	public static bool ExploreWarpPoints(ref bool __result, GameLocation l, List<string> route) {
		if (!Config.Enabled || !Config.Extras.OptimizeWarpPoints) {
			return true;
		}

		if (RoutesFromLocationToLocationSet is null) {
			return true;
		}

		// RoutesFromLocationToLocation is always a new list when first entering this method
		var routeList = new ConcurrentBag<List<string>>();

		var locations = new Dictionary<string, GameLocation?>(Game1.locations.Select(location => new KeyValuePair<string, GameLocation?>(location.Name, location)));

		__result = ExploreWarpPointsImpl(l, route, routeList, locations);

		RoutesFromLocationToLocationSet(routeList.ToList());
		foreach (var listedRoute in routeList) {
			var innerRoutes = FasterRouteMap.GetOrAddDefault(listedRoute.First(), () => new Dictionary<string, List<string>>());
			innerRoutes![listedRoute.Last()] = listedRoute;
		}
		return false;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static GameLocation? GetTarget(this Warp? warp, Dictionary<string, GameLocation?> locations) {
		if (warp is null) {
			return null;
		}

		switch (warp.TargetName) {
			case "BoatTunnel":
				return locations.GetValueOrDefault("IslandSouth", null);
			case "Farm":
			case "Woods":
			case "Backwoods":
			case "Tunnel":
				return Config.Extras.AllowNPCsOnFarm ? locations.GetValueOrDefault(warp.TargetName, null) : null;
			case "Volcano":
				return null;
			default:
				return locations.GetValueOrDefault(warp.TargetName, null);
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static GameLocation? GetTarget(this KeyValuePair<XNA.Point, string> door, Dictionary<string, GameLocation?> locations) {
		if (door.Value is null) {
			return null;
		}

		switch (door.Value) {
			case "BoatTunnel":
				return locations.GetValueOrDefault("IslandSouth", null);
			default:
				return locations.GetValueOrDefault(door.Value, null);
		}
	}

	private readonly record struct PointPair(Vector2I Start, Vector2I End);

	private static readonly ConcurrentDictionary<GameLocation, ConcurrentDictionary<PointPair, bool>> CachedPathfindPoints = new();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static List<string>? Dijkstra(GameLocation start, GameLocation end, Dictionary<string, GameLocation?> locations) {
		try {
			NPC? dummyNPC = null;
			foreach (var location in Game1.locations) {
				dummyNPC = location.getCharacters().FirstOrDefault(c => c is NPC);
				if (dummyNPC is not null) {
					break;
				}
			}
			var queue = new SimplePriorityQueue<GameLocation, int>((x, y) => x - y);
			var listDistances = new Dictionary<GameLocation, int>() { { start, 0 } };
			var previous = new Dictionary<GameLocation, GameLocation?>(Game1.locations.Count);
			var startPositions = new Dictionary<GameLocation, Vector2I>();

			queue.Enqueue(start, 0);
			foreach (var location in Game1.locations) {
				if (previous.TryAdd(location, null)) {
					queue.EnqueueWithoutDuplicates(location, int.MaxValue);
					listDistances.TryAdd(location, int.MaxValue);
				}
			}

			while (queue.Count != 0) {
				var distance = queue.GetPriority(queue.First);
				var location = queue.Dequeue();
				var listDistance = listDistances[location];
				Vector2I? entryPosition = null;
				if (startPositions.TryGetValue(location, out var pos)) {
					entryPosition = pos;
				}

				if (distance == int.MaxValue) {
					return null; // No path
				}

				if (location == end) {
					var result = new string[listDistance + 1];

					GameLocation? current = location;
					int insertionIndex = listDistance;
					while (current is not null) {
						result[insertionIndex--] = current.Name;
						current = previous.GetValueOrDefault(current, null);
					}

					return result.BeList();
				}

				void ProcessNeighbor(GameLocation node, Vector2I egress, Vector2I? start) {
					if (!queue.Contains(node)) {
						return;
					}

					int nodeDistance;

					if (Config.Extras.TrueShortestPath && entryPosition is Vector2I currentPos) {
						var straightDistance = (egress - currentPos).LengthSquared;
						nodeDistance = distance + 1 + straightDistance;
					}
					else {
						nodeDistance = distance + 1;
					}
					if (nodeDistance < queue.GetPriority(node)) {
						if (start.HasValue) {
							startPositions[node] = start.Value;
						}
						listDistances[node] = listDistance + 1;
						previous[node] = location;
						queue.UpdatePriority(node, nodeDistance);
					}
				}

				bool IsPointAccessible(GameLocation node, Vector2I point) {
					if (!entryPosition.HasValue) {
						return true;
					}

					var pointDictionary = CachedPathfindPoints.GetOrAdd(node, _ => new());
					if (pointDictionary.TryGetValue(new(entryPosition.Value, point), out var hasPath)) {
						return hasPath;
					}

					bool result;
					lock (node) {
						if (node.Name == "Farm") {
							result = PathFindController.FindPathOnFarm(entryPosition.Value, point, node, int.MaxValue) is not null;
						}
						else {
							result = PathFindController.findPathForNPCSchedules(entryPosition.Value, point, node, int.MaxValue) is not null;
						}
					}
					pointDictionary.TryAdd(new(entryPosition.Value, point), result);

					return result;
				}

				foreach (var warp in location.warps) {
					var neighbor = warp.GetTarget(locations);
					if (neighbor is null) {
						continue;
					}

					if (!IsPointAccessible(location, (warp.X, warp.Y))) {
						continue;
					}

					ProcessNeighbor(neighbor, (warp.X, warp.Y), (warp.TargetX, warp.TargetY));
				}

				foreach (var door in location.doors.Pairs) {
					var neighbor = door.GetTarget(locations);
					if (neighbor is null) {
						continue;
					}

					if (!IsPointAccessible(location, (door.Key.X, door.Key.Y))) {
						continue;
					}

					try {
						var warp = location.getWarpFromDoor(door.Key, dummyNPC);
						ProcessNeighbor(neighbor, (door.Key.X, door.Key.Y), (warp.TargetX, warp.TargetY));
					}
					catch (Exception) {
						ProcessNeighbor(neighbor, (door.Key.X, door.Key.Y), null);
					}
				}
			}
		}
		catch (Exception ex) {
			Debug.Error("Exception generating warp points route list", ex);
		}

		return null; // Also no path
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static bool ExploreWarpPointsImpl(GameLocation startLocation, List<string> route, ConcurrentBag<List<string>> routeList, Dictionary<string, GameLocation?> locations) {
		// This really should be using A*, but simple Dijkstra will work for now
		var foundTargetsSet = new HashSet<string>();
		foundTargetsSet.Add(startLocation.Name);

		foreach (var location in Game1.locations) {
			if (!foundTargetsSet.Add(location.Name)) {
				continue;
			}

			var result = Dijkstra(startLocation, location, locations);
			if (result is null) {
				continue;
			}
			routeList.Add(result);

			for (int len = result.Count - 1; len >= 2; --len) {
				if (!foundTargetsSet.Add(result[len - 1])) {
					break;
				}
				var subList = result.GetRange(0, len);
				routeList.Add(subList);
			}
		}

		return true;
	}
}
