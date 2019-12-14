using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace SpriteMaster.Types
{
	[StructLayout(LayoutKind.Explicit, Pack = 0)]
	struct Texel : ICloneable
	{
		public enum Offset : uint
		{
			Alpha = 3,
			Blue = 2,
			Green = 1,
			Red = 0,

			A = Alpha,
			B = Blue,
			G = Green,
			R = Red
		}

		// C# doesn't allow unsigned shift values. I have no idea why.
		public enum Shift : int
		{
			Alpha = 8 * (int)Offset.Alpha,
			Blue = 8 * (int)Offset.Blue,
			Green = 8 * (int)Offset.Green,
			Red = 8 * (int)Offset.Red,

			A = Alpha,
			B = Blue,
			G = Green,
			R = Red
		}

		public enum Mask : uint
		{
			Alpha = 0xFFU << Shift.Alpha,
			Blue = 0xFFU << Shift.Blue,
			Green = 0xFFU << Shift.Green,
			Red = 0xFFU << Shift.Red,

			A = Alpha,
			B = Blue,
			G = Green,
			R = Red
		}

		public static class Colors {
			public static readonly Texel Transparent = new Texel() { UPacked = 0x00000000 };
			public static readonly Texel Black = new Texel() { UPacked = 0xFF000000 };
			public static readonly Texel White = new Texel() { UPacked = 0xFFFFFFFF };
			public static readonly Texel Blue = new Texel() { UPacked = 0xFFFF0000 };
			public static readonly Texel Green = new Texel() { UPacked = 0xFF00FF00 };
			public static readonly Texel Red = new Texel() { UPacked = 0xFF0000FF };
			public static readonly Texel Magenta = new Texel() { UPacked = 0xFFFF00FF };
		}

		[FieldOffset(0)]
		public int Packed;
		[FieldOffset(0)]
		public uint UPacked;

		[FieldOffset(3 - (int)Offset.A)]
		public byte A;
		[FieldOffset(3 - (int)Offset.A)]
		public byte Alpha;

		[FieldOffset(3 - (int)Offset.B)]
		public byte B;
		[FieldOffset(3 - (int)Offset.B)]
		public byte Blue;

		[FieldOffset(3 - (int)Offset.G)]
		public byte G;
		[FieldOffset(3 - (int)Offset.G)]
		public byte Green;

		[FieldOffset(3 - (int)Offset.R)]
		public byte R;
		[FieldOffset(3 - (int)Offset.R)]
		public byte Red;

		public bool IsTransparent
		{
			get { return A != 0; }
		}

		public Color Color
		{
			readonly get { return Color.FromArgb(A, R, G, B); }
			set { A = value.A; R = value.R; G = value.G; B = value.B; }
		}

		internal static Texel From(in Color color)
		{
			return new Texel()
			{
				Color = color
			};
		}

		internal static Texel FromARGB(in Color color)
		{
			return new Texel()
			{
				A = color.A,
				B = color.R,
				G = color.G,
				R = color.B
			};
		}

		public readonly Texel Clone()
		{
			return new Texel() { UPacked = UPacked };
		}

		readonly object ICloneable.Clone()
		{
			return Clone();
		}
	}
}
