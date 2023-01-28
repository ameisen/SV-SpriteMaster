using System;

namespace MusicMaster;

internal static class GameConstants {
	internal static class FrameTime {
		internal const int Nanoseconds = 16_666_667; // default 60hz
		internal const int Ticks = Nanoseconds / 100;
		internal static readonly TimeSpan TimeSpan = new(Ticks);
	}
}
