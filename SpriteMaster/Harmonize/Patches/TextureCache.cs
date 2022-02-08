using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Pastel;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using static SpriteMaster.Harmonize.Harmonize;

namespace SpriteMaster.Harmonize.Patches;

static class TextureCache {
	private static readonly ConcurrentDictionary<string, WeakReference<XTexture2D>> TextureCacheTable = new();
	private static readonly ConditionalWeakTable<XTexture2D, string> TexturePaths = new();
	private static readonly WeakSet<XTexture2D> PremultipliedTable = new();

	[Harmonize(typeof(XTexture2D), "FromStream", Harmonize.Fixation.Prefix, PriorityLevel.Last, platform: Harmonize.Platform.MonoGame, instance: false)]
	public static bool FromStreamPre(ref XTexture2D __result, GraphicsDevice graphicsDevice, FileStream stream) {
		if (!Config.SMAPI.TextureCacheEnabled) {
			return true;
		}

		using var watchdogScoped = WatchDog.WatchDog.ScopedWorkingState;

		if (graphicsDevice is not null && stream is not null) {
			var path = stream.Name;
			if (TextureCacheTable.TryGetValue(path, out var textureRef) && textureRef.TryGetTarget(out var texture)) {
				if (texture.IsDisposed || texture.GraphicsDevice != graphicsDevice) {
					TextureCacheTable.TryRemove(path, out var _);
					TexturePaths.Remove(texture);
				}
				else {
					Debug.Trace($"Found Texture2D for '{path}' in cache!".Pastel(System.Drawing.Color.LightCyan));
					__result = texture;
					return false;
				}
			}
		}

		return true;
	}

	[Harmonize(typeof(XTexture2D), "FromStream", Harmonize.Fixation.Postfix, PriorityLevel.Last, platform: Harmonize.Platform.MonoGame, instance: false)]
	public static void FromStreamPost(ref XTexture2D __result, GraphicsDevice graphicsDevice, FileStream stream) {
		if (!Config.SMAPI.TextureCacheEnabled) {
			return;
		}

		using var watchdogScoped = WatchDog.WatchDog.ScopedWorkingState;

		if (__result is null || stream is null) {
			return;
		}

		var result = __result;
		PremultipliedTable.Remove(result);
		TextureCacheTable.AddOrUpdate(stream.Name, result.MakeWeak(), (name, original) => result.MakeWeak());
		TexturePaths.AddOrUpdate(result, stream.Name);
	}

	private static readonly ThreadLocal<WeakReference<XTexture2D>> CurrentPremultiplyingTexture = new();

	[Harmonize(
		typeof(StardewModdingAPI.Framework.ModLoading.RewriteFacades.AccessToolsFacade),
		"StardewModdingAPI.Framework.ContentManagers.ModContentManager",
		"PremultiplyTransparency",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.First
	)]
	public static bool PremultiplyTransparencyPre(ContentManager __instance, ref XTexture2D __result, XTexture2D texture) {
		if (!Config.SMAPI.TextureCacheEnabled) {
			return true;
		}

		if (PremultipliedTable.Contains(texture)) {
			__result = texture;
			CurrentPremultiplyingTexture.Value = null!;
			return false;
		}

		CurrentPremultiplyingTexture.Value = texture.MakeWeak();
		return true;
	}

	[Harmonize(
		typeof(StardewModdingAPI.Framework.ModLoading.RewriteFacades.AccessToolsFacade),
		"StardewModdingAPI.Framework.ContentManagers.ModContentManager",
		"PremultiplyTransparency",
		Harmonize.Fixation.Postfix,
		Harmonize.PriorityLevel.First
	)]
	public static void PremultiplyTransparencyPost(ContentManager __instance, ref XTexture2D __result, XTexture2D texture) {
		if (!Config.SMAPI.TextureCacheEnabled) {
			return;
		}

		PremultipliedTable.AddOrIgnore(texture);
		CurrentPremultiplyingTexture.Value = null!;
	}

	internal static void Remove(XTexture2D texture) {
		if (!Config.SMAPI.TextureCacheEnabled) {
			return;
		}

		// Prevent an annoying circular logic problem
		if (CurrentPremultiplyingTexture.Value?.TryGetTarget(out var currentTexture) ?? false && currentTexture == texture) {
			return;
		}

		PremultipliedTable.Remove(texture);
		if (TexturePaths.TryGetValue(texture, out var path)) {
			TextureCacheTable.TryRemove(path, out var _);
			TexturePaths.Remove(texture);
		}
	}
}
