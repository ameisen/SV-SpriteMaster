using SpriteMaster.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpriteMaster.Configuration.Preview;

class BasicTexture : MetaTexture {
	internal Vector2I Size => new(Texture.Width, Texture.Height);
	internal Vector2I RenderedSize => Size * 4;

	internal BasicTexture(
		string textureName
	) : base(textureName) {
	}
}
