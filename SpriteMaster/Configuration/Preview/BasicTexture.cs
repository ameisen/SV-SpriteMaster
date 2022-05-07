using SpriteMaster.Types;

namespace SpriteMaster.Configuration.Preview;

internal class BasicTexture : MetaTexture {
	internal Vector2I Size => new(Texture.Width, Texture.Height);
	internal Vector2I RenderedSize => Size * 4;

	internal BasicTexture(
		string textureName
	) : base(textureName) {
	}
}
