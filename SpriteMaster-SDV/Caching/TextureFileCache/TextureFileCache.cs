using JetBrains.Annotations;
using SpriteMaster.Types;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SpriteMaster.Caching;

internal static partial class TextureFileCache {
	[StructLayout(LayoutKind.Auto)]
	private readonly struct RawTextureData : IRawTextureData {
		private readonly Vector2I Size;
		private readonly XColor[] Data;

		[Pure]
		readonly int IRawTextureData.Width => Size.Width;
		[Pure]
		readonly int IRawTextureData.Height => Size.Height;
		[Pure]
		readonly XColor[] IRawTextureData.Data => Data;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal RawTextureData(Vector2I size, XColor[] data) {
			Size = size;
			Data = data;
		}
	}

	[MustUseReturnValue, MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool OnLoadRawImageData(LocalizedContentManager __instance, ref IRawTextureData __result, FileInfo file, bool forRawData) {
		if (!Stb.Enabled || !SMConfig.TextureFileCache.Enabled) {
			return true;
		}

		string resolvedPath = Path.GetFullPath(file.FullName);

		if (GetRawImageData(resolvedPath, forRawData) is { } data) {
			__result = new RawTextureData(
				size: data.Size,
				data: data.Data
			);

			return false;
		}

		return !LoadFromFile(path: resolvedPath, copyArray: forRawData, swallowExceptions: false, out __result!);
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	private static bool LoadFromFile(string path, bool copyArray, bool swallowExceptions, [NotNullWhen(true)] out IRawTextureData? result) {
		if (LoadFromFile(path, copyArray, swallowExceptions) is { } data) {
			result = new RawTextureData(
				size: data.Size,
				data.Data
			);

			return true;
		}

		result = null;
		return false;
	}

	private static string? GetModsPath() {
		const BindingFlags smapiBindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

		// Try to use reflection first.
		Type apiConstants = typeof(StardewModdingAPI.Constants);
		if (apiConstants.GetProperty("ModsPath", smapiBindingFlags)?.GetValue(null) is string modsPath && Directory.Exists(modsPath)) {
			return modsPath;
		}
		if (apiConstants.GetProperty("DefaultModsPath", smapiBindingFlags)?.GetValue(null) is string defaultModsPath && Directory.Exists(defaultModsPath)) {
			return defaultModsPath;
		}

		string? rootDirectory = Path.GetDirectoryName(SpriteMaster.Assembly.Location);

		bool IsModsDirectory() {
			return File.Exists(Path.Combine(rootDirectory, "Stardew Valley.dll"));
		}

		string? previousDirectory = rootDirectory;
		while (rootDirectory is not null && rootDirectory.Length != 0 && !IsModsDirectory()) {
			previousDirectory = rootDirectory;
			rootDirectory = Path.GetDirectoryName(rootDirectory);
		}

		if (rootDirectory is not null) {
			return previousDirectory!;
		}

		return null;
	}
}
