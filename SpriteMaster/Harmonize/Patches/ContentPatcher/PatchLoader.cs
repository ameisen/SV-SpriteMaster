#if false

using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static SpriteMaster.Harmonize.Harmonize;

namespace SpriteMaster.Harmonize.Patches.SMAPI;

internal static class PatchLoader {
	private static readonly ThreadLocal<bool> IsReverse = new(false);

	[MethodImpl(MethodImplOptions.NoInlining)]
	[Harmonize(
		"System.IO.File",
		"Exists",
		fixation: Fixation.Reverse,
		critical: false,
		instance: false
	)]
	public static bool ExistsReverse(string? path) {
		IsReverse.Value = true;
		try {
			return System.IO.File.Exists(path);
		}
		finally {
			IsReverse.Value = false;
		}
	}

	private static readonly Dictionary<string, bool> FileExistenceCache = new(StringComparer.OrdinalIgnoreCase);
	private static ulong Hits = 0;
	private static ulong Misses = 0;
	private static ulong Empty = 0;
	private static ulong Total => Interlocked.Read(ref Hits) + Interlocked.Read(ref Misses) + Interlocked.Read(ref Empty);


	private static readonly HashSet<string> AlreadyPreloading = new(StringComparer.OrdinalIgnoreCase);
	private static void StartPreloadCache(string path, bool directory = false) {
		if (!directory) {
			if (Path.GetDirectoryName(path) is { } directoryPath) {
				path = directoryPath;
			}
			else {
				return;
			}
		}

		if (path.Length == 0 || path == ".") {
			path = Directory.GetCurrentDirectory();
		}

		if (
			!directory &&
			!path.StartsWith(StardewModdingAPI.Constants.ContentPath) &&
			!path.StartsWith(StardewModdingAPI.Constants.DataPath) &&
			!path.StartsWith(StardewModdingAPI.Constants.GamePath) &&
			!path.StartsWith(StardewModdingAPI.Constants.LogDir) &&
			!path.StartsWith(StardewModdingAPI.Constants.SavesPath) &&
			!path.StartsWith(Directory.GetCurrentDirectory())
		) {
			return;
		}


		lock (AlreadyPreloading) {
			if (!AlreadyPreloading.Add(path)) {
				return;
			}
		}

		_ = Task.Run(() => PreloadCache(path));
	}

	private static void PreloadCache(string path) {
		var directoryInfo = new DirectoryInfo(path);
		if (!directoryInfo.Exists) {
			return;
		}

		try {
			foreach (var child in directoryInfo.EnumerateFileSystemInfos()) {
				try {
					if (child is DirectoryInfo childDirectory) {
						StartPreloadCache(childDirectory.FullName, directory: true);
					}
					else if (child is FileInfo childFile) {
						PreloadFile(childFile);
					}
				}
				catch {
					// swallow exceptions
				}
			}
		}
		catch {
			// swallow exceptions
		}
	}

	private static void PreloadFile(FileInfo file) {
		var path = file.FullName;
		bool result = ExistsReverse(path);
		lock (FileExistenceCache) {
			_ = FileExistenceCache.TryAdd(path, result);
		}
	}

	[Harmonize(
		"System.IO.File",
		"Exists",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		critical: false,
		instance: false
	)]
	public static bool Exists(string? path, ref bool __result) {
		if (string.IsNullOrEmpty(path)) {
			__result = false;
			return false;
		}

		if (IsReverse.Value) {
			return true;
		}

		__result = ExistsInternal(path);

		return false;
	}

	[MustUseReturnValue, MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ExistsInternal(string? path) {
		if (string.IsNullOrEmpty(path)) {
			_ = Interlocked.Increment(ref Empty);
			return false;
		}

		if (path.Length >= 2 && path[0] == '.' && path[1] is '/' or '\\') {
			path = $"{Directory.GetCurrentDirectory()}{path.Substring(1)}";
		}

		lock (FileExistenceCache) {
			if (FileExistenceCache.TryGetValue(path, out bool exists)) {
				_ = Interlocked.Increment(ref Hits);
				return exists;
			}
		}

		bool result = ExistsReverse(path);
		StartPreloadCache(path);
		lock (FileExistenceCache) {
			_ = FileExistenceCache.TryAdd(path, result);
		}

		_ = Interlocked.Increment(ref Misses);
		return result;
	}

	[HarmonizeTranspile(
		"ContentPatcher.Framework.Patches.Patch",
		"NormalizeLocalAssetPath",
		argumentTypes: new[] { typeof(string), typeof(string) },
		instance: true,
		forMod: "Pathoschild.ContentPatcher"
	)]
	public static IEnumerable<CodeInstruction> NormalizeLocalAssetPathTranspiler(IEnumerable<CodeInstruction> instructions) {
		var newMethod = new Func<string?, bool>(ExistsInternal).Method;
		var oldMethod = new Func<string?, bool>(System.IO.File.Exists).Method;

		var codeInstructions = instructions as CodeInstruction[] ?? instructions.ToArray();

		bool applied = false;

		IEnumerable<CodeInstruction> ApplyPatch() {
			short callOpCode = OpCodes.Call.Value;

			foreach (var instruction in codeInstructions) {
				if (
					instruction.opcode.Value == callOpCode &&
					ReferenceEquals(instruction.operand, oldMethod)
				) {
					yield return new(instruction) {
						operand = newMethod
					};
					applied = true;
				}
				else {
					yield return instruction;
				}
			}
		}

		var result = ApplyPatch().ToArray();

		if (!applied) {
			Debug.Error("Could not apply NormalizeLocalAssetPath File.Exists optimization patch");
		}

		return result;
	}

	/*
	[Harmonize(
		"ContentPatcher.Framework.Patches.Patch",
		"NormalizeLocalAssetPath",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		critical: false
	)]
	public static bool NormalizeLocalAssetPath() {
		return false;
	}
	*/
}

#endif
