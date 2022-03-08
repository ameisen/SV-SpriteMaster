using Microsoft.Toolkit.HighPerformance;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpriteMaster;

static partial class Debug {
	static internal class Mode {
		[Flags]
		internal enum DebugModeFlags {
			None = 0,
			Select = 1 << 0
		}

		internal static DebugModeFlags CurrentMode = DebugModeFlags.None;

		internal static bool IsModeEnabled(DebugModeFlags mode) => (CurrentMode & mode) == mode;

		internal static void EnableMode(DebugModeFlags mode) => CurrentMode |= mode;

		internal static void DisableMode(DebugModeFlags mode) => CurrentMode &= ~mode;

		internal static void ToggleMode(DebugModeFlags mode) => CurrentMode ^= mode;

		private static Vector2I CurrentCursorPosition {
			get {
				var mouseRaw = (Vector2I)StardewValley.Game1.getMousePositionRaw();

				if (Game1.uiMode) {
					var screenRatio = (Vector2F)(Vector2I)StardewValley.Game1.uiViewport.Size / (Vector2F)(Vector2I)StardewValley.Game1.viewport.Size;
					return ((Vector2F)mouseRaw * screenRatio).NearestInt();
				}
				else {
					return StardewValley.Game1.getMousePositionRaw();
				}
			}
		}

		private readonly struct DrawInfo {
			internal readonly XNA.Graphics.Texture2D Texture;
			internal readonly Bounds Destination;
			internal readonly Bounds Source;
			internal readonly XNA.Color Color;
			internal readonly float Rotation;
			internal readonly Vector2F Origin;
			internal readonly XNA.Graphics.SpriteEffects Effects;
			internal readonly float LayerDepth;

			internal DrawInfo(
				XNA.Graphics.Texture2D texture,
				in Bounds destination,
				in Bounds source,
				in XNA.Color color,
				float rotation,
				Vector2F origin,
				XNA.Graphics.SpriteEffects effects,
				float layerDepth
			) {
				Texture = texture;
				Destination = destination;
				Source = source;
				Color = color;
				Rotation = rotation;
				Origin = origin;
				Effects = effects;
				LayerDepth = layerDepth;
			}
		}

		private static readonly List<DrawInfo> SelectedDraws = new();

		internal static bool RegisterDrawForSelect(
			XNA.Graphics.Texture2D texture,
			in Bounds destination,
			in Bounds source,
			in XNA.Color color,
			float rotation,
			Vector2F origin,
			XNA.Graphics.SpriteEffects effects,
			float layerDepth
		) {
			if (!IsModeEnabled(DebugModeFlags.Select)) {
				return false;
			}

			var currentCursor = CurrentCursorPosition;
			if (currentCursor == destination.Offset) {
				if (texture == Game1.mouseCursors || texture == Game1.mouseCursors2) {
					return false;
				}
				if (texture is ManagedTexture2D managedTexture && managedTexture.Reference.TryGetTarget(out var reference) && (reference == Game1.mouseCursors || reference == Game1.mouseCursors2)) {
					return false;
				}
			}

			var realDestination = destination;
			realDestination.Offset -= origin.NearestInt();

			if (realDestination.Contains(currentCursor)) {
				SelectedDraws.Add(new(
					texture: texture,
					destination: destination,
					source: source,
					color: color,
					rotation: rotation,
					origin: origin,
					effects: effects,
					layerDepth: layerDepth
				));

				return true;
			}

			return false;
		}

		internal static bool RegisterDrawForSelect(
			XNA.Graphics.Texture2D texture,
			Vector2F position,
			in Bounds source,
			in XNA.Color color,
			float rotation,
			Vector2F origin,
			Vector2F scale,
			XNA.Graphics.SpriteEffects effects,
			float layerDepth
		) {
			if (!IsModeEnabled(DebugModeFlags.Select)) {
				return false;
			}

			var roundedPosition = position.NearestInt();
			var roundedExtent = (source.ExtentF * scale).NearestInt();

			Bounds destination = new(
				roundedPosition,
				roundedExtent
			);
			return RegisterDrawForSelect(
				texture: texture,
				destination: destination,
				source: source,
				color: color,
				rotation: rotation,
				origin: origin * scale,
				effects: effects,
				layerDepth: layerDepth
			);
			// new Bounds(((Vector2F)adjustedPosition).NearestInt(), (sourceRectangle.ExtentF * adjustedScale).NearestInt())
		}

		internal static bool Draw() {
			if (!IsModeEnabled(DebugModeFlags.Select)) {
				return false;
			}

			if (SelectedDraws.Count == 0) {
				return false;
			}

			Game1.spriteBatch.Begin();

			try {
				List<StringBuilder> lines = new(SelectedDraws.Count);
				foreach (var draw in SelectedDraws) {
					var sb = new StringBuilder();
					sb.AppendLine(draw.Texture.NormalizedName());
					sb.AppendLine($"  dst: {draw.Destination}");
					sb.AppendLine($"  src: {draw.Source}");
					sb.AppendLine($"  org: {draw.Origin}");
					lines.Add(sb);
				}

				if (lines.Count != 0) {
					var font = Game1.smallFont;

					int minWidth = Game1.viewport.Width / 5;
					int minHeight = Game1.viewport.Height / 5;
					int totalWidth = 0;
					int totalHeight = 0;
					foreach (var line in lines) {
						var newlines = line.ToString().Count('\n');
						var spacing = 0;// font.LineSpacing * (newlines - 1);
						var lineMeasure = font.MeasureString(line);
						totalWidth = Math.Max(totalWidth, lineMeasure.X.NextInt());
						totalHeight += lineMeasure.Y.NextInt() + spacing;
					}

					if (totalWidth < minWidth) {
						totalWidth = minWidth;
					}

					if (totalHeight < minHeight) {
						totalHeight = minHeight;
					}

					Game1.drawDialogueBox(
							x: 10,
							y: 0,
							width: totalWidth,
							height: (totalHeight * 1.30).NearestInt(),
							speaker: false,
							drawOnlyBox: true,
							message: null,
							objectDialogueWithPortrait: false,
							ignoreTitleSafe: true,
							r: -1,
							g: -1,
							b: -1
					);

					int heightOffset = 0;
					foreach (var line in lines) {
						var newlines = line.ToString().Count('\n');
						var spacing = 0;// font.LineSpacing * (newlines - 1);
						var lineHeight = font.MeasureString(line).Y.NextInt() + spacing;

						Utility.drawTextWithShadow(
							b: Game1.spriteBatch,
							text: line,
							font: font,
							position: new Vector2I(46, heightOffset + 100),
							color: Game1.textColor,
							scale: 1
						);

						heightOffset += lineHeight;
					}
				}
			}
			finally {
				Game1.spriteBatch.End();
				SelectedDraws.Clear();
			}

			return true;
		}
	}
}
