using HarmonyLib;
using JetBrains.Annotations;
using LinqFasterer;
using SpriteMaster.Caching;
using SpriteMaster.Experimental;
using SpriteMaster.Extensions;
using SpriteMaster.GL;
using SpriteMaster.Harmonize;
using SpriteMaster.Harmonize.Patches.Game;
using SpriteMaster.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace SpriteMaster;

public sealed partial class SpriteMaster {
	internal static Assembly Assembly => typeof(SpriteMaster).Assembly;
	private const string UniqueId = "DigitalCarbide.SpriteMaster";

	private static string ModDirectory => Self?.Helper?.DirectoryPath ?? Path.GetDirectoryName(Assembly.Location) ?? Assembly.Location;

	private const string ConfigName = "config.toml";

	internal static SpriteMaster Self { get; private set; } = default!;

	internal readonly MemoryMonitor.Monitor MemoryMonitor;

	private readonly Lazy<Harmony> HarmonyInstance = new(() => new(UniqueId));

	[UsedImplicitly]
	public SpriteMaster() {
		Self.AssertNull();
		Self = this;

		_ = ThreadingExt.IsMainThread;

		Runtime.CorrectProcessorAffinity();

		DirectoryCleanup.Cleanup();

		GLExt.EnableDebugging();

		Initialize();

		Garbage.EnterNonInteractive();

		//SteamHelper.Init();

		MemoryMonitor = new();

		var assemblyPath = Assembly.Location;
		assemblyPath = Path.GetDirectoryName(assemblyPath);

		// Compress our own directory
		if (assemblyPath is not null) {
			DirectoryExt.CompressDirectory(assemblyPath, force: true);
		}

		TextureFileCache.Precache();
	}

	private void InitializeConfig() {
		SMConfig.SetPath(Path.Combine(ModDirectory, ConfigName));

		SMConfig.DefaultConfig = new MemoryStream();
		Configuration.Serialize.Save(SMConfig.DefaultConfig, leaveOpen: true);

		if (!SMConfig.IgnoreConfig) {
			Configuration.Serialize.Load(SMConfig.Path);
		}

		if (Versioning.IsOutdated(SMConfig.ConfigVersion)) {
			Debug.Info($"config.toml is out of date ({SMConfig.ConfigVersion} < {SMConfig.ClearConfigBefore}), rewriting it.");

			Configuration.Serialize.Load(SMConfig.DefaultConfig, retain: true);
			SMConfig.DefaultConfig.Position = 0;
			SMConfig.ConfigVersion = Versioning.CurrentVersion;
		}

		static Regex ProcessTexturePattern(string pattern) {
			pattern = pattern.StartsWith('@') ?
				pattern[1..] :
				$"^{Regex.Escape(pattern)}.*";
			return new(pattern, RegexOptions.Compiled);
		}

		static SMConfig.TextureRef[] ProcessTextureRefs(List<string> textureRefStrings) {
			// handle sliced textures. At some point I will add struct support.
			var result = new SMConfig.TextureRef[textureRefStrings.Count];
			for (int i = 0; i < result.Length; ++i) {
				var slicedTexture = textureRefStrings[i];
				//@"LooseSprites\Cursors::0,640:2000,256"
				var elements = slicedTexture.Split("::", 2);
				var texture = elements[0];
				var bounds = Bounds.Empty;
				if (elements.Length > 1) {
					try {
						var boundElements = elements[1].Split(':');
						var offsetElements = (boundElements.ElementAtOrDefaultF(0) ?? "0,0").Split(',', 2);
						var extentElements = (boundElements.ElementAtOrDefaultF(1) ?? "0,0").Split(',', 2);

						var offset = new Vector2I(int.Parse(offsetElements[0]), int.Parse(offsetElements[1]));
						var extent = new Vector2I(int.Parse(extentElements[0]), int.Parse(extentElements[1]));

						bounds = new(offset, extent);
					}
					catch {
						Debug.Error($"Invalid SlicedTexture Bounds: '{elements[1]}'");
					}
				}
				result[i] = new(ProcessTexturePattern(texture), bounds);
			}
			return result;
		}

		SMConfig.Resample.SlicedTexturesS = ProcessTextureRefs(SMConfig.Resample.SlicedTextures);
		SMConfig.Resample.Padding.BlackListS = ProcessTextureRefs(SMConfig.Resample.Padding.BlackList);

		// Compile blacklist patterns
		static Regex[] ProcessTexturePatterns(List<string> texturePatternStrings) {
			var result = new Regex[texturePatternStrings.Count];
			for (int i = 0; i < texturePatternStrings.Count; ++i) {
				result[i] = ProcessTexturePattern(texturePatternStrings[i]);
			}
			return result;
		}


		SMConfig.Resample.BlacklistPatterns = ProcessTexturePatterns(SMConfig.Resample.Blacklist);
		SMConfig.Resample.GradientBlacklistPatterns = ProcessTexturePatterns(SMConfig.Resample.GradientBlacklist);
	}

	private bool Initialized = false;
	private void Initialize() {
		Runtime.CorrectProcessorAffinity();

		if (Initialized) {
			ConfigureHarmony(early: false);
			return;
		}

		try {
			Debug.Message(Versioning.StringHeader);

			ConfigureHarmony(early: true);

			InitializeConfig();

			Initialized = true;
		}
		catch {
			// Swallow Exceptions
		}
	}

	private void PostInitialize() {
		MemoryMonitor.Start();

		RuntimeHelpers.RunClassConstructor(typeof(FileCache).TypeHandle);
		WatchDog.WatchDog.Initialize();
		ClickCrash.Initialize();
	}

	private static void ForceGarbageCollect() {
		Garbage.Collect(compact: true, blocking: true, background: false);
	}

	private static void ForceGarbageCollectConcurrent() {
		Garbage.Collect(compact: false, blocking: false, background: true);
	}

	private void ConfigureHarmony(bool early) {
		bool wasInitialized = HarmonyInstance.IsValueCreated;

		var instance = HarmonyInstance.Value;

		// If early initialization hadn't already occurred, do it now.
		if (!early && !wasInitialized) {
			instance.ApplyPatches(early: true);
		}

		instance.ApplyPatches(early);

		Inlining.Reenable();
	}
}
