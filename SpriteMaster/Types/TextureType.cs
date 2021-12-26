namespace SpriteMaster.Types;

enum TextureType {
	Sprite = 0, // sprite in a spritesheet
	Image = 1, // the entire texture is an image, drawn at once
	SlicedImage = 2, // the entire texture is an image, drawn in slices
}
