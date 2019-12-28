﻿using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.IO;
using TeximpNet.Compression;

namespace SpriteMaster {
	static class Config {
		internal sealed class CommentAttribute : Attribute {
			public readonly string Message;

			public CommentAttribute (string message) {
				Message = message;
			}
		}

		internal sealed class ConfigIgnoreAttribute : Attribute { }

		internal static readonly string ModuleName = typeof(Config).Namespace;

		internal static bool Enabled = true;
		internal static SButton ToggleButton = SButton.F11;

		internal const int MaxSamplers = 16;
		[ConfigIgnore]
		internal static bool RendererSupportsAsyncOps = true;
		[ConfigIgnore]
		internal static int ClampDimension = BaseMaxTextureDimension; // this is adjustable by the system itself. The user shouldn't be able to touch it.
		[Comment("The preferred maximum texture edge length, if allowed by the hardware")]
		internal const int AbsoluteMaxTextureDimension = 16384;
		internal const int BaseMaxTextureDimension = 4096;
		internal static int PreferredMaxTextureDimension = 8192;
		internal const bool ClampInvalidBounds = true;
		internal const uint MaxMemoryUsage = 2048U * 1024U * 1024U;
		internal const bool EnableCachedHashTextures = false;
		internal const bool IgnoreUnknownTextures = false;
		internal static long ForceGarbageCompactAfter = long.MaxValue;
		internal static long ForceGarbageCollectAfter = long.MaxValue;
		internal static bool GarbageCollectAccountUnownedTextures = true;
		internal static bool GarbageCollectAccountOwnedTexture = true;
		internal static bool DiscardDuplicates = false;
		internal static int DiscardDuplicatesFrameDelay = 2;
		internal static List<string> DiscardDuplicatesBlacklist = new List<string>() {
			"LooseSprites\\Cursors",
			"Minigames\\TitleButtons"
		};

		internal enum Configuration {
			Debug,
			Release
		}

#if DEBUG
		internal const Configuration BuildConfiguration = Configuration.Debug;
#else
		internal const Configuration BuildConfiguration = Configuration.Release;
#endif

		internal const bool IsDebug = BuildConfiguration == Configuration.Debug;
		internal const bool IsRelease = BuildConfiguration == Configuration.Release;

		internal static readonly string LocalRoot = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"StardewValley",
			"Mods",
			ModuleName
		);

		internal static class Debug {
			internal static class Logging {
				internal const bool LogInfo = true;
				internal static bool LogWarnings = true;
				internal static bool LogErrors = true;
				internal const bool OwnLogFile = true;
				internal const bool UseSMAPI = true;
			}

			internal static class Sprite {
				internal const bool DumpReference = false;
				internal const bool DumpResample = false;
			}
		}

		internal static class DrawState {
			internal static bool SetLinear = true;
			internal static bool EnableMSAA = true;
			internal static bool DisableDepthBuffer = true;
			internal static SurfaceFormat BackbufferFormat = SurfaceFormat.Rgba1010102;
		}

		internal static class Resample {
			internal const bool Smoothing = true;
			internal const bool Scale = Smoothing;
			internal static int MaxScale = 5;
			internal static int MinimumTextureDimensions = 4;
			internal const bool EnableWrappedAddressing = true;
			internal const bool UseBlockCompression = true;
			internal static CompressionQuality BlockCompressionQuality = CompressionQuality.Highest;
			internal static int BlockHardAlphaDeviationThreshold = 7;
			internal static List<string> Blacklist = new List<string>() {
				"LooseSprites\\Lighting\\"
			};
			internal static class Padding {
				internal const bool Enabled = true;
				internal static int MinimumSizeTexels = 4;
				internal const bool IgnoreUnknown = false;
				internal static List<string> StrictList = new List<string>() {
					"LooseSprites\\Cursors"
				};
				internal static List<string> Whitelist = new List<string>() {
					"LooseSprites\\font_bold",
					"Characters\\Farmer\\hairstyles",
					"Characters\\Farmer\\pants",
					"Characters\\Farmer\\shirts",
					"TileSheets\\weapons",
					"TileSheets\\bushes",
					"TerrainFeatures\\grass",
					"TileSheets\\debris",
					"TileSheets\\animations",
					"Maps\\springobjects",
					"Maps\\summerobjects",
					"Maps\\winterobjects",
					"Maps\\fallobjects",
					"Buildings\\houses",
					"TileSheets\\furniture",
					"TerrainFeatures\\tree1_spring",
					"TerrainFeatures\\tree2_spring",
					"TerrainFeatures\\tree3_spring",
					"TerrainFeatures\\tree1_summer",
					"TerrainFeatures\\tree2_summer",
					"TerrainFeatures\\tree3_summer",
					"TerrainFeatures\\tree1_fall",
					"TerrainFeatures\\tree2_fall",
					"TerrainFeatures\\tree3_fall",
					"TerrainFeatures\\tree1_winter",
					"TerrainFeatures\\tree2_winter",
					"TerrainFeatures\\tree3_winter",
				};
				internal static List<string> Blacklist = new List<string>() {
				};
			}
		}

		internal static class WrapDetection {
			internal const bool Enabled = true;
			internal const float edgeThreshold = 0.2f;
			internal static byte alphaThreshold = 1;
		}

		internal static class AsyncScaling {
			internal static bool Enabled = true;
			internal static bool EnabledForUnknownTextures = false;
			internal const bool CanFetchAndLoadSameFrame = false;
			internal const int MaxLoadsPerFrame = 1;
			internal static long MinimumSizeTexels = 0;
			internal static long ScalingBudgetPerFrameTexels = 2 * 256 * 256;
			internal const int MaxInFlightTasks = int.MaxValue; // Environment.ProcessorCount;
		}

		internal static class MemoryCache {
			internal static bool Enabled = true;
			internal enum Algorithm {
				None = 0,
				LZ = 1,
				LZMA = 2
			}
			internal static Algorithm Type = Algorithm.LZ;
		}

		internal static class Cache {
			internal static bool Enabled = true;
			internal const int LockRetries = 32;
			internal const int LockSleepMS = 32;
		}
	}
}
