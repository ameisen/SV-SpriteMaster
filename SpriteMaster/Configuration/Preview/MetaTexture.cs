using System;

namespace SpriteMaster.Configuration.Preview;

internal abstract class MetaTexture : IDisposable {
	internal readonly XTexture2D Texture;

	protected MetaTexture(XTexture2D? texture) {
		if (texture is null) {
			throw new NullReferenceException(nameof(texture));
		}
		Texture = texture;
	}

	protected MetaTexture(string textureName) : this(StardewValley.Game1.content.Load<XTexture2D>(textureName)) { }

	~MetaTexture() {
		Dispose(false);
	}

	internal void Dispose(bool disposing) {
		if (disposing) {
			Texture?.Dispose();
			GC.SuppressFinalize(this);
		}
	}

	public void Dispose() {
		Dispose(true);
	}
}
