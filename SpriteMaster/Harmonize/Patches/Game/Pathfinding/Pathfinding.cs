using Priority_Queue;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using StardewValley;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static StardewValley.PathFindController;

namespace SpriteMaster.Harmonize.Patches.Game.Pathfinding;

using DoorPair = KeyValuePair<XNA.Point, string>;

static partial class Pathfinding {
	private static readonly Action<List<List<string>>>? RoutesFromLocationToLocationSet = typeof(NPC).GetFieldSetter<List<List<string>>>("routesFromLocationToLocation");
	private static readonly Dictionary<string, Dictionary<string, List<string>>> FasterRouteMap = new();

	static Pathfinding() {
		if (RoutesFromLocationToLocationSet is null) {
			Debug.Warning($"Could not find 'NPC.routesFromLocationToLocation'");
		}
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
	private static GameLocation? GetTarget(this DoorPair door, Dictionary<string, GameLocation?> locations) {
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
							result = FindPathOnFarm(entryPosition.Value, point, node, int.MaxValue) is not null;
						}
						else {
							result = findPathForNPCSchedules(entryPosition.Value, point, node, int.MaxValue) is not null;
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
