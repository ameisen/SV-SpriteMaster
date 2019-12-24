using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Metadata;
using SpriteMaster.Types;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SpriteMaster.HarmonyExt.Patches.PSpriteBatch {
	[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Harmony")]
	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Harmony")]
	static class Draw {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Cleanup (this ref Rectangle sourceRectangle, Texture2D reference) {
			if (Config.ClampInvalidBounds) {
				sourceRectangle = sourceRectangle.ClampTo(new Rectangle(0, 0, reference.Width, reference.Height));
			}

			// Let's just skip potentially invalid draws since I have no idea what to do with them.
			return (sourceRectangle.Height > 0 && sourceRectangle.Width > 0);
		}

		private static ScaledTexture FetchScaledTexture (
			this SpriteBatch @this,
			Texture2D reference,
			ref Rectangle sourceRectangle,
			bool create = false
		) {
			try {
				if (!sourceRectangle.Cleanup(reference))
					return null;

				if (reference is RenderTarget2D || reference.Width < 1 || reference.Height < 1)
					return null;

				if (reference.Extent().MaxOf <= Config.Resample.MinimumTextureDimensions)
					return null;

				var scaledTexture = create ?
					ScaledTexture.Get(reference, sourceRectangle) :
					ScaledTexture.Fetch(reference, sourceRectangle);
				if (scaledTexture != null && scaledTexture.IsReady) {
					var t = scaledTexture.Texture;

					if (t == null || t.IsDisposed)
						return null;

					sourceRectangle = (Bounds)t.Dimensions;

					return scaledTexture;
				}
			}
			catch (Exception ex) {
				ex.PrintError();
			}

			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ScaledTexture GetScaledTexture (
			this SpriteBatch @this,
			Texture2D reference,
			ref Rectangle sourceRectangle
		) {
			var scaledTexture = @this.FetchScaledTexture(reference, ref sourceRectangle, create: true);
			if (scaledTexture != null) {
				return scaledTexture;
			}

			return null;
		}

		// Takes the arguments, and checks to see if the texture is padded. If it is, it is forwarded to the correct draw call, avoiding
		// intervening mods altering the arguments first.
		internal static bool OnDrawFirst (
			this SpriteBatch @this,
			ref Texture2D texture,
			ref Rectangle destination,
			ref Rectangle? source,
			Color color,
			float rotation,
			ref Vector2 origin,
			SpriteEffects effects,
			float layerDepth
		) {
			texture.Meta().UpdateLastAccess();
			var sourceRectangle = source.GetValueOrDefault(new Rectangle(0, 0, texture.Width, texture.Height));
			var originalSourceRectangle = sourceRectangle;

			var scaledTexture = GetScaledTexture(@this, texture, ref sourceRectangle);
			if (scaledTexture == null) {
				return true;
			}

			scaledTexture.UpdateReferenceFrame();

			if (!scaledTexture.Padding.IsZero) {
				// Convert the draw into the other draw style. This has to be done because the padding potentially has
				// subpixel accuracy when scaled to the destination rectangle.

				var originalSize = new Vector2(originalSourceRectangle.Width, originalSourceRectangle.Height);
				var destinationSize = new Vector2(destination.Width, destination.Height);
				var newScale = destinationSize / originalSize;
				var newPosition = new Vector2(destination.X, destination.Y);

				@this.Draw(
					texture: texture,
					position: newPosition,
					sourceRectangle: source,
					color: color,
					rotation: rotation,
					origin: origin,
					scale: newScale,
					effects: effects,
					layerDepth: layerDepth
				);
				return false;
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool OnDraw (
			this SpriteBatch @this,
			ref Texture2D texture,
			ref Rectangle destination,
			ref Rectangle? source,
			Color color,
			float rotation,
			ref Vector2 origin,
			SpriteEffects effects,
			float layerDepth
		) {
			texture.Meta().UpdateLastAccess();
			var sourceRectangle = source.GetValueOrDefault(new Rectangle(0, 0, texture.Width, texture.Height));

			var scaledTexture = FetchScaledTexture(@this, texture, ref sourceRectangle);
			if (scaledTexture == null) {
				return true;
			}

			scaledTexture.UpdateReferenceFrame();

			var t = scaledTexture.Texture;

			var scaledOrigin = origin / scaledTexture.Scale;

			source = sourceRectangle;
			origin = scaledOrigin;
			texture = t;

			return true;
		}

		internal static bool OnDraw (
			this SpriteBatch @this,
			ref Texture2D texture,
			ref Vector2 position,
			ref Rectangle? source,
			Color color,
			float rotation,
			ref Vector2 origin,
			ref Vector2 scale,
			SpriteEffects effects,
			float layerDepth
		) {
			texture.Meta().UpdateLastAccess();

			var sourceRectangle = source.GetValueOrDefault(new Rectangle(0, 0, texture.Width, texture.Height));
			bool allowPadding = true;

			ScaledTexture scaledTexture;
			Texture2D t;
			if (texture is ScaledTexture.ManagedTexture2D managedTexture) {
				scaledTexture = managedTexture.Texture;
				t = texture;
			}
			else {
				scaledTexture = FetchScaledTexture(@this, texture, ref sourceRectangle, allowPadding);
				if (scaledTexture == null) {
					return true;
				}
				t = scaledTexture.Texture;
			}

			scaledTexture.UpdateReferenceFrame();

			var adjustedScale = scale / scaledTexture.Scale;
			var adjustedPosition = position;
			var adjustedOrigin = origin;

			if (!scaledTexture.Padding.IsZero) {
				var textureSize = new Vector2(sourceRectangle.Width, sourceRectangle.Height);
				var innerSize = (Vector2)scaledTexture.UnpaddedSize;

				// This is the scale factor to bring the inner size to the draw size.
				var innerRatio = textureSize / innerSize;

				// Scale the... scale by the scale factor.
				adjustedScale *= innerRatio;

				adjustedOrigin *= scaledTexture.Scale;
				adjustedOrigin /= innerRatio;
				adjustedOrigin += (textureSize - innerSize) * 0.5f;
			}
			else {
				adjustedOrigin *= scaledTexture.Scale;
			}

			texture = t;
			source = sourceRectangle;
			origin = adjustedOrigin;
			scale = adjustedScale;
			position = adjustedPosition;
			return true;
		}
	}
}
