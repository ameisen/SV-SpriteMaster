using SpriteMaster.Types.Fixed;
using System;
using System.Runtime.InteropServices;

#nullable enable

namespace SpriteMaster.Types;

[StructLayout(LayoutKind.Explicit, Pack = sizeof(ulong), Size = sizeof(ulong))]
struct Color16 {
	internal static readonly Color16 Zero = new(0UL);

	[FieldOffset(0)]
	internal ulong Packed = 0;

	[FieldOffset(0)]
	internal Fixed16 R = 0;
	[FieldOffset(2)]
	internal Fixed16 G = 0;
	[FieldOffset(4)]
	internal Fixed16 B = 0;
	[FieldOffset(8)]
	internal Fixed16 A = 0;

	private static Color16 From(Color8 color) => new(
		(Fixed16)color.R,
		(Fixed16)color.G,
		(Fixed16)color.B,
		(Fixed16)color.A
	);

	internal Color16(ulong rgba) : this() {
		Packed = rgba;
	}

	internal Color16(in (ushort R, ushort G, ushort B) color) : this(color.R, color.G, color.B) { }

	internal Color16(ushort r, ushort g, ushort b) : this() {
		R = r;
		G = g;
		B = b;
	}

	internal Color16(in (Fixed16 R, Fixed16 G, Fixed16 B) color) : this(color.R, color.G, color.B) { }

	internal Color16(Fixed16 r, Fixed16 g, Fixed16 b) : this() {
		R = r;
		G = g;
		B = b;
	}

	internal Color16(in (ushort R, ushort G, ushort B, ushort A) color) : this(color.R, color.G, color.B, color.A) { }

	internal Color16(ushort r, ushort g, ushort b, ushort a) : this() {
		R = r;
		G = g;
		B = b;
		A = a;
	}

	internal Color16(in (Fixed16 R, Fixed16 G, Fixed16 B, Fixed16 A) color) : this(color.R, color.G, color.B, color.A) { }

	internal Color16(Fixed16 r, Fixed16 g, Fixed16 b, Fixed16 a) : this() {
		R = r;
		G = g;
		B = b;
		A = a;
	}

	public static explicit operator ulong(Color16 value) => value.Packed;
	public static explicit operator Color16(ulong value) => new(value);

	internal static unsafe void Convert(Color8* source, Color16* destination, int count) {
		for (int i = 0; i < count; ++i) {
			destination[i] = From(source[i]);
		}
	}

	internal static void Convert(ReadOnlySpan<Color8> source, Span<Color16> destination, int count) {
		for (int i = 0; i < count; ++i) {
			destination[i] = From(source[i]);
		}
	}

	internal static Span<Color16> Convert(ReadOnlySpan<Color8> source, bool pinned = true) {
		var destination = new Span<Color16>(GC.AllocateUninitializedArray<Color16>(source.Length, pinned: pinned));
		for (int i = 0; i < source.Length; ++i) {
			destination[i] = From(source[i]);
		}
		return destination;
	}
}
