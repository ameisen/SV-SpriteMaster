using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System;
using System.IO;

namespace SpriteMaster {
	static class Config {
		internal class FileIgnoreAttribute : Attribute { }

		internal static readonly string ModuleName = typeof(Config).Namespace;

		internal static bool Enabled = true;
		internal static SButton ToggleButton = SButton.F11;

		internal const int MaxSamplers = 16;
		[FileIgnore]
		internal static int ClampDimension = 4096; // this is adjustable by the mod
		internal static int PreferredMaxTextureDimension = 8192;
		internal const bool RestrictSize = false;
		internal const bool ClampInvalidBounds = true;
		internal const uint MaxMemoryUsage = 2048U * 1024U * 1024U;
		internal const bool EnableCachedHashTextures = false;
		internal const bool IgnoreUnknownTextures = false;

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

			internal static class CacheDump {
				internal const bool Enabled = IsDebug;
				internal const SButton Button = SButton.F10;
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
			internal const bool SmartScale = true;
			internal static int MaxScale = 5;
			internal const bool DeSprite = true;
			internal const bool EnableWrappedAddressing = true;
			internal static class Padding {
				internal const bool Enabled = true;
				internal const int MinSize = 4;
				internal const bool IgnoreUnknown = true;
			}
		}

		internal static class WrapDetection {
			internal const bool Enabled = true;
			internal const float edgeThreshold = 0.25f;
			internal const byte alphaThreshold = 1;
		}

		internal static class AsyncScaling {
			internal static bool Enabled = true;
			internal static bool CanFetchAndLoadSameFrame = true;
			internal static int MaxLoadsPerFrame = 2;
			internal static int ScalingBudgetPerFrame = 2 * 256 * 256;
		}

		internal static class Cache {
			internal static bool Enabled = true;
			internal const int LockRetries = 32;
			internal const int LockSleepMS = 32;
		}
	}
}
