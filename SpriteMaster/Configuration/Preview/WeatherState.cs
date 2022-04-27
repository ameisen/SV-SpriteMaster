using StardewValley;
using System;
using System.Collections.Generic;
using static SpriteMaster.Harmonize.Patches.Game.Snow;

namespace SpriteMaster.Configuration.Preview;

struct WeatherState : IDisposable {
	// SDV
	internal readonly bool IsDebrisWeather { get; init; }
	internal readonly List<WeatherDebris> DebrisWeather { get; init; }

	// SM
	internal readonly SnowState SnowWeatherState { get; init; }

	internal static WeatherState Backup() => new() {
		IsDebrisWeather = Game1.isDebrisWeather,
		DebrisWeather = Game1.debrisWeather,

		SnowWeatherState = SnowState.Backup()
	};

	internal readonly void Restore() {
		Game1.isDebrisWeather = IsDebrisWeather;
		Game1.debrisWeather = DebrisWeather;

		SnowWeatherState.Restore();
	}

	public readonly void Dispose() => Restore();
}
