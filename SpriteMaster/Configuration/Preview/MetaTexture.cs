using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Configuration.Preview;

internal abstract class MetaTexture : IDisposable {
	internal readonly XTexture2D Texture;

	[DoesNotReturn]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static T ThrowNullReferenceException<T>(string name) =>
		throw new NullReferenceException(name);

	protected MetaTexture(XTexture2D? texture) {
		Texture = texture ?? ThrowNullReferenceException<XTexture2D?>(nameof(texture));
	}

	protected MetaTexture(string textureName) : this(StardewValley.Game1.content.Load<XTexture2D>(textureName)) { }

	~MetaTexture() {
		Dispose(false);
	}

	internal void Dispose(bool disposing) {
		if (disposing) {
			Texture.Dispose();
			GC.SuppressFinalize(this);
		}
	}

	public void Dispose() {
		Dispose(true);
	}
}
