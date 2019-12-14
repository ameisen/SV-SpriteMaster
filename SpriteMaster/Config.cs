using StardewModdingAPI;
using System;
using System.IO;

namespace SpriteMaster
{
	static class Config
	{
		internal static readonly string ModuleName = typeof(Config).Namespace;

		internal static bool Enabled = true;
		internal const SButton ToggleButton = SButton.F11;

		internal const int MaxSamplers = 16;
		internal static int ClampDimension = 4096;
		internal const int PreferredDimension = 8192;
		internal const bool RestrictSize = false;
		internal const bool ClampInvalidBounds = true;
		internal const uint MaxMemoryUsage = 2048U * 1024U * 1024U;
		internal const bool EnableCachedHashTextures = false;

		internal static readonly string LocalRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Stardew Valley", ModuleName);

		internal static class Debug
		{
			internal static class Logging
			{
				internal const bool LogInfo = true;
				internal const bool LogWarnings = true;
				internal const bool LogErrors = true;
				internal const bool OwnLogFile = true;
			}

			internal static class TextureDump
			{
				internal const bool Enabled = true;
				internal const SButton Button = SButton.F10;
			}

			internal static class Sprite
			{
				internal const bool DumpReference = false;
				internal const bool DumpResample = false;
			}
		}

		internal static class DrawState
		{
			internal const bool SetLinear = true;
		}

		internal static class Resample
		{
			internal const bool Smoothing = true;
			internal const bool Scale = Smoothing;
			internal const bool SmartScale = true;
			internal const int MaxScale = 5;
			internal const bool DeSprite = true;
			internal const bool EnableWrappedAddressing = true;
		}

		internal static class WrapDetection
		{
			internal const bool Enabled = true;
			internal const float edgeThreshold = 0.5f;
			internal const byte alphaThreshold = 128;
		}

		internal static class AsyncScaling
		{
			internal const bool Enabled = true;
			internal const bool CanFetchAndLoadSameFrame = true;
			internal const int MaxLoadsPerFrame = 1;
			internal const int TexelFetchFrameBudget = 128 * 128;
		}
	}
}
