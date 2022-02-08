using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using System;

namespace SpriteMaster.Harmonize.Patches;

static class Snow {
	[Harmonize(
		typeof(Game1),
		"drawWeather",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last
	)]
	public static bool DrawWeather(Game1 __instance, GameTime time, RenderTarget2D target_screen) {
		if (!Config.Enabled || !Config.Extras.CustomSnow) {
			return true;
		}

		// Is it snow?
		bool drawSnow = Game1.IsSnowingHere() && Game1.currentLocation.isOutdoors && Game1.currentLocation is not Desert;
		if (drawSnow) {
			if (__instance.takingMapScreenshot) {
				if (Game1.debrisWeather is not null) {
					foreach (WeatherDebris w in Game1.debrisWeather) {
						Vector2 position = w.position;
						w.position = new Vector2(Game1.random.Next(Game1.viewport.Width - w.sourceRect.Width * 3), Game1.random.Next(Game1.viewport.Height - w.sourceRect.Height * 3));
						Game1.spriteBatch.Draw(Game1.mouseCursors, w.position, w.sourceRect, Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1E-06f);
						w.position = position;
					}
				}
			}
			else if (Game1.viewport.X > -Game1.viewport.Width) {
				foreach (WeatherDebris item in Game1.debrisWeather) {
					Game1.spriteBatch.Draw(Game1.mouseCursors, item.position, item.sourceRect, Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 1E-06f);
				}
			}

			return false;
		}
		return true;
	}

	[Harmonize(
		typeof(Game1),
		"updateWeather",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false
	)]
	public static bool UpdateWeather(GameTime time) {
		if (!Config.Enabled || !Config.Extras.CustomSnow) {
			return true;
		}

		// Is it snow?
		bool drawSnow = Game1.IsSnowingHere() && Game1.currentLocation.isOutdoors && Game1.currentLocation is not Desert;
		if (drawSnow) {
			if (Game1.currentSeason == "fall" && Game1.random.NextDouble() < 0.001 && Game1.windGust == 0f && WeatherDebris.globalWind >= -0.5f) {
				Game1.windGust += (float)Game1.random.Next(-10, -1) / 100f;
				if (Game1.soundBank != null) {
					Game1.wind = Game1.soundBank.GetCue("wind");
					Game1.wind.Play();
				}
			}
			else if (Game1.windGust != 0f) {
				Game1.windGust = Math.Max(-5f, Game1.windGust * 1.02f);
				WeatherDebris.globalWind = -0.5f + Game1.windGust;
				if (Game1.windGust < -0.2f && Game1.random.NextDouble() < 0.007) {
					Game1.windGust = 0f;
				}
			}
			foreach (WeatherDebris item in Game1.debrisWeather) {
				item.update();
			}

			return false;
		}
		return true;
	}

	[Harmonize(
		typeof(Game1),
		"updateRainDropPositionForPlayerMovement",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false
	)]
	public static bool UpdateRainDropPositionForPlayerMovement(int direction, bool overrideConstraints, float speed) {
		if (!Config.Enabled || !Config.Extras.CustomSnow) {
			return true;
		}

		if (!overrideConstraints && (!Game1.IsSnowingHere() || !Game1.currentLocation.IsOutdoors || (direction != 0 && direction != 2 && (Game1.player.getStandingX() < Game1.viewport.Width / 2 || Game1.player.getStandingX() > Game1.currentLocation.Map.DisplayWidth - Game1.viewport.Width / 2)) || (direction != 1 && direction != 3 && (Game1.player.getStandingY() < Game1.viewport.Height / 2 || Game1.player.getStandingY() > Game1.currentLocation.Map.DisplayHeight - Game1.viewport.Height / 2)))) {
			return true;
		}

		// Is it snow?
		bool drawSnow = Game1.IsSnowingHere() && Game1.currentLocation.isOutdoors && Game1.currentLocation is not Desert;
		if (drawSnow) {
			Game1.updateDebrisWeatherForMovement(Game1.debrisWeather, direction, overrideConstraints, speed);
			return false;
		}
		return true;
	}

	[Harmonize(
		typeof(Game1),
		"populateDebrisWeatherArray",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last,
		instance: false
	)]
	public static bool PopulateDebrisWeatherArray() {
		if (!Config.Enabled || !Config.Extras.CustomSnow) {
			return true;
		}

		if (!Game1.IsSnowingHere()) {
			return true;
		}

		Game1.isDebrisWeather = true;
		int debrisToMake = Game1.random.Next(Config.Extras.MinimumSnowDensity, Config.Extras.MaximumSnowDensity);
		int baseIndex = 3;
		Game1.debrisWeather.Clear();
		Game1.debrisWeather.Capacity = debrisToMake;
		for (int i = 0; i < debrisToMake; i++) {
			Game1.debrisWeather.Add(
				new WeatherDebris(
					new Vector2(Game1.random.Next(0, Game1.viewport.Width), Game1.random.Next(0, Game1.viewport.Height)),
					baseIndex,
					(float)Game1.random.Next(15) / 500f,
					(float)Game1.random.Next(-10, 0) / 50f,
					(float)Game1.random.Next(10) / 50f
				)
			);
		}

		return false;
	}

	/*
	private static readonly Lazy<Texture2D> FishTexture = new(() => Game1.content.Load<Texture2D>("LooseSprites\\AquariumFish"));
	[Harmonize(
		typeof(StardewValley.WeatherDebris),
		"draw",
		Harmonize.Fixation.Prefix,
		Harmonize.PriorityLevel.Last
	)]
	public static bool Draw(WeatherDebris __instance, SpriteBatch b) {
		if (!Config.Enabled || !Config.Extras.CustomSnow) {
			return true;
		}

		var source = __instance.sourceRect;
		source.Location -= new Point(352, 1216);
		source.Location = new Point(source.Location.X % 16, source.Location.Y % 16);

		b.Draw(FishTexture.Value, __instance.position, source, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1E-06f);

		return false;
	}
	*/
}
