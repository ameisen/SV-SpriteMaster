using JetBrains.Annotations;
using SpriteMaster.Types;

namespace SpriteMaster;

[PublicAPI]
public struct Quad() {
	public int Left = 0;
	public int Right = 0;
	public int Top = 0;
	public int Bottom = 0;

	internal Quad(scoped in Vector2I horizontal, scoped in Vector2I vertical) : this() {
		Left = horizontal.X;
		Right = horizontal.Y;
		Top = vertical.X;
		Bottom = vertical.Y;
	}
}
