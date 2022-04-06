using System;

namespace SpriteMaster.Types;

partial struct Color8 : IEquatable<DrawingColor> {
	// ToArgb

	public static implicit operator DrawingColor(in Color8 value) => DrawingColor.FromArgb((int)value.ARGB);
	public static implicit operator Color8(DrawingColor value) => new(r: value.R, g: value.G, b: value.B, a: value.A);

	public bool Equals(System.Drawing.Color other) => this.Equals((Color8)other);
}
