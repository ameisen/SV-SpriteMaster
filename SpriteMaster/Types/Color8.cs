using SpriteMaster.Types.Fixed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpriteMaster.Types;

[StructLayout(LayoutKind.Explicit, Pack = sizeof(uint), Size = sizeof(uint))]
struct Color8 {
	[FieldOffset(0)]
	internal uint Packed = 0;

	[FieldOffset(0)]
	internal Fixed8 R = 0;
	[FieldOffset(1)]
	internal Fixed8 G = 0;
	[FieldOffset(2)]
	internal Fixed8 B = 0;
	[FieldOffset(3)]
	internal Fixed8 A = 0;

	private static Color8 From(Color16 color) => new(
		(Fixed8)color.R,
		(Fixed8)color.G,
		(Fixed8)color.B,
		(Fixed8)color.A
	);

	internal Color8(uint rgba) : this() {
		Packed = rgba;
	}

	internal Color8(in (byte R, byte G, byte B) color) : this(color.R, color.G, color.B) {}

	internal Color8(byte r, byte g, byte b) : this() {
		R = r;
		G = g;
		B = b;
	}

	internal Color8(in (Fixed8 R, Fixed8 G, Fixed8 B) color) : this(color.R, color.G, color.B) { }

	internal Color8(Fixed8 r, Fixed8 g, Fixed8 b) : this() {
		R = r;
		G = g;
		B = b;
	}

	internal Color8(in (byte R, byte G, byte B, byte A) color) : this(color.R, color.G, color.B, color.A) { }

	internal Color8(byte r, byte g, byte b, byte a) : this() {
		R = r;
		G = g;
		B = b;
		A = a;
	}

	internal Color8(in (Fixed8 R, Fixed8 G, Fixed8 B, Fixed8 A) color) : this(color.R, color.G, color.B, color.A) { }

	internal Color8(Fixed8 r, Fixed8 g, Fixed8 b, Fixed8 a) : this() {
		R = r;
		G = g;
		B = b;
		A = a;
	}

	public static explicit operator uint(Color8 value) => value.Packed;
	public static explicit operator Color8(uint value) => new(value);

	internal static unsafe void Convert(Color16* source, Color8* destination, int count) {
		for (int i = 0; i < count; ++i) {
			destination[i] = From(source[i]);
		}
	}

	internal static void Convert(ReadOnlySpan<Color16> source, Span<Color8> destination, int count) {
		for (int i = 0; i < count; ++i) {
			destination[i] = From(source[i]);
		}
	}

	internal static Span<Color8> Convert(ReadOnlySpan<Color16> source, bool pinned = true) {
		var destination = new Span<Color8>(GC.AllocateUninitializedArray<Color8>(source.Length, pinned: pinned));
		for (int i = 0; i < source.Length; ++i) {
			destination[i] = From(source[i]);
		}
		return destination;
	}
}
